# 🐶 Pixel Dog Reminders

> A personalized retro pixel-art desktop companion & habit reminder app for Windows, built with WPF & .NET 8.

---

## ✨ Features

- **🏠 Home Companion**: Live digital clock, time-aware dynamic greetings, and an interactive pixel dog that loves petting, eating snacks, and strolling across your screen.
- **⏰ Habit Reminders**: Multi-slot and interval-based reminders (Food, Water, Sleep, Meds, Breaks) with 2-minute neutral auto-dismiss.
- **⚽🏎️ Sports & Match Alerts**: Real-time 30-day schedule alerts for FC Barcelona fixtures and Formula 1 Grand Prix sessions.
- **🚶 Morning Walk-In Greeting**: Once per morning (6:00 AM – 11:59 AM), the companion walks across the bottom of the screen to greet you with **Feed** & **Let it be** reactions.
- **🚀 Windows Startup & Tray Resident**: Launches silently into the background system tray on boot.

---

## 🛠️ Tech Stack

- **Framework**: WPF + C# (.NET 8 Windows)
- **Pixel Art**: Custom 32×32 grid sprite engine generated with zero-dependency pure Python scripts (51 frames across 9 variants).
- **APIs**: Jolpica Ergast F1 API (No auth required) & Football-Data.org API.
- **Testing**: xUnit test suite.

---

## 🚀 How to Build & Run

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or newer (targeting `net8.0-windows`)
- Windows 10 / 11

### Run Development Build
```bash
dotnet run
```

### Run Unit Tests
```bash
dotnet test Tests/PixelDogReminders.Tests.csproj
```

### Publish Self-Contained Standalone Executable
```bash
dotnet publish PixelDogReminders.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
```
The output executable will be created at `publish/PixelDogReminders.exe`.
