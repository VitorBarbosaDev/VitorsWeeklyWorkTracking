# ⏱️ Vitor's Weekly Work Tracking

<div align="center">

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-13.0-239120?style=for-the-badge&logo=csharp&logoColor=white)
![WPF](https://img.shields.io/badge/Platform-Windows%20WPF-0078D7?style=for-the-badge&logo=windows&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

**A modern, feature-packed work-hour tracker, Pomodoro focus companion, and productivity planner for Windows.**

[Features](#-features) • [Installation & Build](#-installation--build) • [Getting Started](#-getting-started) • [Tech Stack](#-tech-stack)

</div>

---

## 🌟 Overview

**Vitor's Weekly Work Tracking** is a comprehensive desktop productivity application built with WPF and .NET 9. It combines flexible real-time time tracking, structured Pomodoro focus cycles, weekly work planning, daily welcome kickoffs, visual & ASCII companion animations, gamified focus progression, celebration milestones, detailed analytics with interactive charts, and floating mini-widgets to keep your workday organized, healthy, and motivating.

---
## Screenshots

<img width="919" height="748" alt="image" src="https://github.com/user-attachments/assets/ecfe0df3-3482-404f-a050-1233e593f40a" />
<img width="880" height="281" alt="image" src="https://github.com/user-attachments/assets/aa51549a-b5ff-41f9-a150-c316d14da258" />
<img width="948" height="704" alt="image" src="https://github.com/user-attachments/assets/d2118e93-8ea4-4f95-9b33-8656532219a7" />
<img width="936" height="704" alt="image" src="https://github.com/user-attachments/assets/82c66a56-c946-4113-a7d5-8854891f77ff" />

---
## ✨ Features

### 👋 Daily Welcome & Scheduled Agenda
- **Personalized Daily Greeting**: Greets you on first launch each day with customizable user name personalization (`👋 Welcome back, [Name]!`).
- **Today's Scheduled Goals Overview**: Automatically pulls planned tasks from your Calendar Planner, displaying project targets, notes, and total planned hours.
- **Daily Motivational Quotes**: Fresh, encouraging daily quotes to set a positive tone for your workday.
- **Empty-State Planning Prompt**: Gentle reminders and one-click shortcuts to schedule tasks on days with no set goals.
- **Instant Actions**: Start your day immediately or jump directly into the Calendar Planner.
- **Greeting Preview**: Test and preview your welcome popup directly from the Settings dialog anytime.

### ⏱️ Time Tracking & Project Management
- **One-Click Real-time Tracking**: Start, pause, resume, and stop timers instantly.
- **Hierarchical Structure**: Organize your work by **Projects** and **Activities / Tasks**.
- **Manual Time Adjustments**: Add, edit, or remove past time entries with ease.
- **CSV Data Export**: Export your logs and detailed summaries for reporting, invoicing, or backups.

### 🍅 Pomodoro & Focus Intervals
- **Configurable Work/Rest Cycles**: Customizable Focus duration, Short Break, Long Break, and Cycle limits.
- **Built-in Presets**: Quick-switch between Classic Pomodoro (25/5), Long Focus (50/10), Sprint sessions, and custom intervals.
- **Phase Notifications & Sound Effects**: Procedurally synthesized retro and ambient chimes for focus starts, break transitions, and goal milestones.
- **Interactive Rest Alerts**: Prompts with hydration and stretching reminders during breaks.
- **Celebration Effects**: Themed celebrations and visual confetti rewards upon completing milestones and focus sessions.
- **Gamified Focus XP & Streaks**: Earn focus XP, level up your profile, and build daily focus streaks.

### 🎨 Visual & ASCII Companion Modes
- **Animated Focus Scenes**: Dynamic vector animations and ASCII art scenes that advance as you work:
  - 🚴 **Boy Cycling Home**: Scenic trail ride with a puppy in the basket towards a cozy home.
  - ☕ **Rainy Window Coffee**: Calming raindrops, rising coffee steam, and window fog.
  - 🎷 **Coffee Jazz Window**: Lakeside view beside a warm latte and drifting golden sparkles.
  - 🍦 **Pastel Ice Cream Truck**: Cheerful neighborhood truck with visiting customers.
  - 🎧 **Tokyo Metro Lo-Fi Girl**: Chill vibes riding the metro past glowing city lights.
  - 🐱 **Playful Focus Kitty**: Kitten playing with yarn and chasing treats.
  - 🚀 **Space Rocket Launch**: Blasting off through starry galaxies towards the Moon.
  - 🏃 **Chibi Marathon Runner**: Sprinting along the track toward the victory cup.
  - 👾 **Focus Pet Tamagotchi**: Virtual pocket companion that levels up and glitters as you focus.
  - 🌲 **Lumberjack Wood Chopping**: Hardworking lumberjack chops down a dense forest tree by tree, packing his van full of timber logs to take home.
- **ASCII Engine Mode**: Switch any scene to retro terminal-style ASCII character rendering.

### 📊 Analytics & Reporting
- **Interactive Pie & Ring Charts**: Breakdown of time spent across projects and tasks.
- **Daily & Weekly Comparisons**: Compare planned target hours against actual tracked time.
- **Trend Visualizations**: Detailed historical progress to identify peak productive periods.

### 📌 Floating Mini Timer Widget
- **Always-on-Top Floating Widget**: Minimalist HUD showing active task, elapsed time, and Pomodoro progress.
- **Multi-Monitor Support**: Snap to bottom-right or bottom-left corners across any connected display.

### 📅 Calendar & Goal Planner
- **Target Setting**: Set daily and weekly target hours per project with task notes.
- **Daily Welcome Sync**: Seamlessly populates your daily scheduled goals on startup.
- **Progress Cards**: Live completion rings and motivational indicators directly on the main dashboard.

### 🎨 11 Handcrafted Themes & Themed UI
- **Instant Palette Switching**: Real-time UI re-coloring across 11 vibrant themes:
  - ☀️ **Modern Light**
  - 🖤 **Pitch Black (OLED)**
  - 🌙 **Dark Slate**
  - 🔮 **Midnight Neon**
  - ⚡ **Cyberpunk Synthwave**
  - 💻 **Matrix Terminal**
  - ❄️ **Nordic Frost**
  - 🌲 **Emerald Forest**
  - 🌅 **Sunset Glow**
  - ☕ **Warm Espresso**
  - 🍬 **Candy Pop**
- **Themed Title Bars & Dialogs**: Native Windows DWM title bar styling, custom themed message boxes (`ThemedMessageBox`), and dialogs matching the active theme.
- **Single-Instance Application**: Safe single-instance startup with automatic window activation if the app is already open.

---

## 🚀 Installation & Build

### Prerequisites
- **Windows 10 / 11** (x64 / ARM64)
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- Visual Studio 2022 / JetBrains Rider (optional, for development)

### Clone & Run

```bash
# Clone the repository
git clone https://github.com/your-username/VitorsWeeklyWorkTracking.git
cd VitorsWeeklyWorkTracking

# Restore dependencies and build
dotnet build -c Release

# Run the application
dotnet run --project VitorsWeeklyWorkTracking.csproj
```

### Building the Windows Installer (.msi / .exe)

To generate a full Windows installer package with Desktop shortcuts, Start Menu integration, and clean uninstallation:

#### Option 1: One-Click Build Script
Double-click `build-installer.cmd` or run in PowerShell:

```powershell
.\build-installer.ps1
```

This will automatically publish the self-contained application and generate:
- **`bin/Release/VitorsWeeklyWorkTracking-Setup-x64.msi`** (Native Windows Installer via WiX Toolset)
- **`bin/Release/VitorsWeeklyWorkTracking-Setup-x64.exe`** (Standard Setup Wizard via Inno Setup 6, if installed)

#### Option 2: Manual WiX MSI Build

```powershell
# 1. Publish self-contained executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "bin\Release\net9.0-windows\win-x64\publish"

# 2. Build MSI package
wix build -ext WixToolset.UI.wixext -d SourceDir="." -d PublishDir="bin\Release\net9.0-windows\win-x64\publish" -arch x64 installer\Package.wxs -out "bin\Release\VitorsWeeklyWorkTracking-Setup-x64.msi"
```

### Publishing a Standalone Executable (Without Installer)

To generate a single, self-contained Windows executable without an installer:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The output binary will be located in `bin/Release/net9.0-windows/win-x64/publish/`.

---

## 📖 Getting Started

1. **Set Up Your Profile & Preferences**: Click **⚙️ Settings** to enter your name, configure focus/break durations, customize audio chimes, and preview your Daily Welcome greeting.
2. **Plan Your Work Schedule**: Open the **📅 Calendar Planner** to schedule your target hours and project notes across the week.
3. **Daily Welcome Kickoff**: On first launch each day, review your scheduled agenda, total planned hours, and motivational quote in the **Welcome Back** modal.
4. **Create Projects & Activities**: Use *Project Management* or *Activity Management* to organize custom categories and tasks.
5. **Start Tracking & Stay Focused**: Select your current task from the dropdown and hit **Start** to track time with live vector/ASCII companions and structured Pomodoro intervals.
6. **Use the Floating Mini Widget**: Switch to the floating mini-widget HUD for unobtrusive always-on-top tracking across any monitor.
7. **Review Analytics & Celebrate**: Open **Analytics** to view interactive pie charts, daily comparisons, XP streaks, or export reports to CSV.

---

## 🛠️ Tech Stack

- **Framework**: [.NET 9.0](https://dotnet.microsoft.com/) (WPF - Windows Presentation Foundation)
- **Language**: [C# 13](https://learn.microsoft.com/en-us/dotnet/csharp/)
- **Audio Engine**: Synthesized Procedural Sound Effects (System.Media / AudioSynthesis)
- **Storage**: Local JSON-based persistent storage (`System.Text.Json`)
- **UI Architecture**: Custom XAML vector controls, Dynamic Resource Theme Manager, custom Canvas-based animations.

---


## 📄 License

This project is open source and available under the [MIT License](LICENSE).
