# ⏱️ Vitor's Weekly Work Tracking

<div align="center">

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-13.0-239120?style=for-the-badge&logo=csharp&logoColor=white)
![WPF](https://img.shields.io/badge/Platform-Windows%20WPF-0078D7?style=for-the-badge&logo=windows&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

**A modern, feature-packed work-hour tracker, Pomodoro focus companion, and productivity planner for Windows.**

[Features](#-features) • [Visual Companion & Themes](#-visual-companion--themes) • [Installation & Build](#-installation--build) • [Getting Started](#-getting-started) • [Tech Stack](#-tech-stack)

</div>

---

## 🌟 Overview

**Vitor's Weekly Work Tracking** is a comprehensive desktop productivity application built with WPF and .NET 9. It combines flexible time tracking, structured Pomodoro focus cycles, weekly work planning, visual & ASCII companion animations, gamified focus progression, detailed analytics with interactive charts, and floating mini-widgets to keep your workday organized and motivating.

---

## ✨ Features

### ⏱️ Time Tracking & Project Management
- **One-Click Real-time Tracking**: Start, pause, resume, and stop timers instantly.
- **Hierarchical Structure**: Organize your work by **Projects** and **Activities / Tasks**.
- **Manual Time Adjustments**: Add, edit, or remove past time entries with ease.
- **CSV Data Export**: Export your logs and detailed summaries for reporting, invoicing, or backups.

### 🍅 Pomodoro & Focus Intervals
- **Configurable Work/Rest Cycles**: Customizable Focus duration, Short Break, Long Break, and Cycle limits.
- **Built-in Presets**: Quick-switch between Classic Pomodoro (25/5), Long Focus (50/10), Sprint sessions, and custom intervals.
- **Phase Notifications & Sound Effects**: Procedurally synthesized retro and ambient chimes for focus starts, break transitions, and goal milestones.
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
- **ASCII Engine Mode**: Switch any scene to retro terminal-style ASCII character rendering.

### 📊 Analytics & Reporting
- **Interactive Pie & Ring Charts**: Breakdown of time spent across projects and tasks.
- **Daily & Weekly Comparisons**: Compare planned target hours against actual tracked time.
- **Trend Visualizations**: Detailed historical progress to identify peak productive periods.

### 📌 Floating Mini Timer Widget
- **Always-on-Top Floating Widget**: Minimalist HUD showing active task, elapsed time, and Pomodoro progress.
- **Multi-Monitor Support**: Snap to bottom-right or bottom-left corners across any connected display.

### 📅 Calendar & Goal Planner
- **Target Setting**: Set daily and weekly target hours per project.
- **Progress Cards**: Live completion rings and motivational indicators directly on the main dashboard.

### 🎨 11 Handcrafted Themes
Switch themes instantly with real-time UI re-coloring:
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

### Publishing a Standalone Executable

To generate a single, self-contained Windows executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The output binary will be located in `bin/Release/net9.0-windows/win-x64/publish/`.

---

## 📖 Getting Started

1. **Create Projects & Activities**: Open *Project Management* or *Activity Management* to set up the tasks you will track.
2. **Start Tracking**: Select your current task from the dropdown and hit **Start**.
3. **Enable Pomodoro Mode**: Turn on Interval Mode to structure your day into focus and rest periods.
4. **Choose Your Companion**: Pick an animated visual companion and theme that matches your vibe.
5. **Review Analytics**: Check your daily/weekly breakdown in the **Analytics** window or export your entries to CSV.

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
