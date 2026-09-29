[CmdletBinding()]
param (
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$ProjectFile = "VitorsWeeklyWorkTracking.csproj"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = $PSScriptRoot
$PublishDir = Join-Path $ProjectRoot "bin\$Configuration\net9.0-windows\$Runtime\publish"
$OutputDir = Join-Path $ProjectRoot "bin\$Configuration"
$InstallerDir = Join-Path $ProjectRoot "installer"
$WxsFile = Join-Path $InstallerDir "Package.wxs"
$IssFile = Join-Path $InstallerDir "installer.iss"
$MsiOutput = Join-Path $OutputDir "VitorsWeeklyWorkTracking-Setup-x64.msi"
$ExeOutput = Join-Path $OutputDir "VitorsWeeklyWorkTracking-Setup-x64.exe"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host " Vitor's Weekly Work Tracking - Installer Build " -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# 1. Publish self-contained executable
Write-Host "`n[1/3] Publishing self-contained application ($Runtime, $Configuration)..." -ForegroundColor Yellow
if (Test-Path $PublishDir) {
    Remove-Item -Path $PublishDir -Recurse -Force -ErrorAction SilentlyContinue
}

$publishArgs = @(
    "publish",
    $ProjectFile,
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-o", $PublishDir
)

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to publish application."
    exit $LASTEXITCODE
}
Write-Host "[OK] Application published successfully to: $PublishDir" -ForegroundColor Green

# 2. Build WiX MSI Installer
Write-Host "`n[2/3] Building Windows Installer (.msi)..." -ForegroundColor Yellow

$wixCmd = Get-Command "wix" -ErrorAction SilentlyContinue
if (-not $wixCmd) {
    Write-Host "WiX CLI tool not found in PATH. Attempting to install via dotnet tool..." -ForegroundColor Yellow
    dotnet tool install --global wix
    $wixCmd = Get-Command "wix" -ErrorAction SilentlyContinue
}

if ($wixCmd) {
    & wix eula accept wix7 2>$null
    & wix extension add WixToolset.UI.wixext 2>$null

    & wix build -ext WixToolset.UI.wixext -d SourceDir="$ProjectRoot" -d PublishDir="$PublishDir" -arch x64 "$WxsFile" -out "$MsiOutput"
    if ($LASTEXITCODE -eq 0 -and (Test-Path $MsiOutput)) {
        $msiBytes = (Get-Item $MsiOutput).Length
        $msiMb = [math]::Round($msiBytes / 1048576, 2)
        Write-Host "[OK] MSI Installer created: $MsiOutput ($msiMb MB)" -ForegroundColor Green
    } else {
        Write-Warning "WiX MSI build returned non-zero exit code: $LASTEXITCODE"
    }
} else {
    Write-Warning "WiX CLI is not available. Skipping MSI build."
}

# 3. Build Inno Setup EXE Installer (if ISCC is available)
Write-Host "`n[3/3] Checking for Inno Setup compiler (ISCC)..." -ForegroundColor Yellow

$isccCandidates = @(
    (Get-Command "iscc" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "$env:USERPROFILE\scoop\shims\iscc.exe"
) | Where-Object { $_ -and (Test-Path $_) }

if ($isccCandidates.Count -gt 0) {
    $isccPath = $isccCandidates[0]
    Write-Host "Found Inno Setup compiler at: $isccPath" -ForegroundColor Cyan
    & "$isccPath" "$IssFile"
    if ($LASTEXITCODE -eq 0 -and (Test-Path $ExeOutput)) {
        $exeBytes = (Get-Item $ExeOutput).Length
        $exeMb = [math]::Round($exeBytes / 1048576, 2)
        Write-Host "[OK] Inno Setup EXE created: $ExeOutput ($exeMb MB)" -ForegroundColor Green
    } else {
        Write-Warning "Inno Setup compilation returned non-zero exit code: $LASTEXITCODE"
    }
} else {
    Write-Host "Inno Setup compiler (ISCC.exe) was not found on this machine." -ForegroundColor Gray
    Write-Host "(Note: The WiX MSI installer was generated above. To also build Setup.exe, install Inno Setup 6 from https://jrsoftware.org/isdl.php)" -ForegroundColor Gray
}

Write-Host "`n==================================================" -ForegroundColor Cyan
Write-Host " Build Complete! Artifacts:" -ForegroundColor Green
if (Test-Path $MsiOutput) {
    $msiItem = Get-Item $MsiOutput
    $msiMb = [math]::Round($msiItem.Length / 1048576, 2)
    Write-Host " - MSI Package: $($msiItem.FullName) ($msiMb MB)" -ForegroundColor White
}
if (Test-Path $ExeOutput) {
    $exeItem = Get-Item $ExeOutput
    $exeMb = [math]::Round($exeItem.Length / 1048576, 2)
    Write-Host " - EXE Package: $($exeItem.FullName) ($exeMb MB)" -ForegroundColor White
}
Write-Host "==================================================" -ForegroundColor Cyan
