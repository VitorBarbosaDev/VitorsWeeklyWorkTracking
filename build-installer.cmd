@echo off
setlocal
echo Starting installer build for Vitor's Weekly Work Tracking...
powershell -ExecutionPolicy Bypass -NoProfile -File "%~dp0build-installer.ps1" %*
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Installer build failed with error code %ERRORLEVEL%.
    pause
    exit /b %ERRORLEVEL%
)
echo.
echo Done!
pause
