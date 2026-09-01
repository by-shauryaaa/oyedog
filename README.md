# 🐶 Oye Dog (Pixel Dog Reminders)

> A retro pixel-art desktop companion, smart class timetable manager & habit reminder app for Windows, built with WPF & .NET 8.

---

## ✨ Key Features

### 🏡 Living Interactive Home Scene
- **4 Time-of-Day Pixel Sceneries**: Full-bleed retro pixel backgrounds (Morning sunrise, Bright Day, Twilight Evening, and Midnight Stars) anchored around a cozy doghouse yard with smooth 800ms crossfade transitions.
- **Layered Ambient Motion**:
  - 🌅 **Morning**: Animated pixel birds gliding across the sunrise sky.
  - ☀️ **Day**: Fluffy pixel clouds drifting across the blue sky.
  - 🌆 **Evening**: Warm doghouse porch lantern pulsing and casting soft amber light.
  - 🌙 **Night**: Twinkling star sparkles and glowing fireflies floating in gentle sine waves.
- **De-Boxed Floating Greeting**: Dynamic time-aware greetings floating directly over the scenery with crisp pixel drop shadow contrast.

### 🐕 Walk-to-Center Companion Entrance & Contextual Actions
- **Dynamic Entrance**: Visiting Home triggers Dogu to walk across the lawn from left to center using an 8-frame walk cycle.
- **Contextual Radial Menu**: Once centered, interactive pet-care action buttons pop into place with bouncy spring animations:
  - 🍖 **Feed Dog**: Dogu crunches on tasty treats.
  - ❤️ **Pet / Play**: Happy tail wags, barks, and affection.
  - 🚶 **Walk Across Screen**: Triggers Dogu to take a stroll across your desktop.

### 📋 Smart Class Timetable System
- **Weekly Grid & List Views**: Color-coded 7-day schedule view with real-time time overlap conflict detection.
- **Free-Form Time Input**: Supports arbitrary class schedules (e.g. 09:40, 10:50) with customizable subject durations and room locations.
- **Pre-Class Fly-By Reminders**: Triggers ahead of upcoming classes based on customizable lead times (5, 10, 15, 30 minutes).

### 🎬 3 Selectable Pre-Class Reminder Animation Styles
Customizable in Settings with a live **"Preview Alert 🎬"** test button:
- 🚩 **Simple (Fly-by Flag)**: Classic blocky pixel flag banner flying across the screen left-to-right (10s duration).
- ☁️ **Cloud (Floating Cloud)**: Pixel-art cloud graphic floating across the screen with gentle sinusoidal vertical bobbing (12s duration).
- 🪧 **Banner (Hanging Billboard)**: Drops in from the top of the monitor suspended by edge-touching threads (0.8s drop), holds in place for 10 seconds, then threads retract to the top while the billboard plummets down through the bottom edge of the screen under simulated gravity (0.8s fall).

### 🕹️ Retro RPG Collapsible Sidebar Navigation
- **2-Column Layout**: Replaces traditional tab bars with a retro RPG pause-menu style sidebar.
- **Smooth Animation**: Expands (`200px`) and collapses (`54px`) with fluid easing, persisting state across sessions.
- **Companion Mascot Habitat**: Houses an enlarged `84×84px` animated companion sprite, grounding shadow, and floating digital clock badge (`♥ [12:00 PM]`).

### 📅 Unified Tray Schedule Timeline
- **Single Left & Right Click Workflow**:
  - **Left-Click** (or double-click): Instantly restores the window and lands on the Home tab.
  - **Right-Click**: Opens a unified glanceable context menu showing today's chronological schedule of classes (with the currently active class in **Bold** `▶ [NOW]`) and sports matches, plus companion actions (*Open*, *Walk In*, *Pause/Resume Reminders*, *Exit*).

### ⏰ Habit Reminders & Sports Hub
- **Habit Reminders**: Multi-slot and interval-based popups for Sleep (11:00 PM), Water (every 2h), Food, and custom tasks with neutral 2-minute auto-dismiss.
- **Live Sports Fixtures**: Automated 30-day schedule alerts for FC Barcelona matches (via Football-Data.org) and Formula 1 Grand Prix sessions (via Jolpica Ergast F1 API).

### 🎉 Secret Custom Display Name Easter Egg
- Clicking the `"♥ made for abhishek ♥"` strip in Settings 4 times unlocks the secret **`✨ CUSTOM COMPANION NAME ✨`** dialog, personalizing greetings, window title, and tray tooltip globally.

---

## 🛠️ Tech Stack & Architecture

- **Framework**: WPF + C# (.NET 8 Windows)
- **UI Architecture**: Multi-view MVVM shell with dynamic `ContentControl` navigation and custom layered WPF animation engines.
- **Pixel Art Assets**: Custom 32×32 grid sprite engine and time-of-day scenery backdrops generated via zero-dependency pure Python scripts (`scripts/render_sprites.py`, `scripts/generate_scenery.py`).
- **Persistence**: Single-document JSON persistence (`%AppData%\PixelDogReminders\config.json`) preserving settings, reminders, and subjects.
- **Installer**: Automated Inno Setup 6 packaging script (`scripts/build_installer.ps1`) generating a standalone 46 MB single-file installer.

---

## 🚀 How to Build & Run

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or newer (targeting `net8.0-windows`)
- Windows 10 / 11

### Run Development Build
```powershell
dotnet run
```

### Run Unit Tests
```powershell
dotnet test Tests/PixelDogReminders.Tests.csproj
```

### Build Windows Installer
```powershell
powershell -ExecutionPolicy Bypass -File scripts\build_installer.ps1
```
The output installer will be created at `publish/installer/OyeDogSetup.exe`.
