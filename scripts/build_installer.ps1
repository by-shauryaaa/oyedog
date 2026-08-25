# Automated Build & Packaging Script for Oye Dog
param(
    [switch]$SkipPublish = $false
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "🐶 Oye Dog — Packaging Windows Installer" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# 1. Generate ICO icon
Write-Host "`n[1/3] Generating app icon..." -ForegroundColor Yellow
python "$RootDir\scripts\generate_ico.py"

# 2. Build & Publish self-contained executable
if (-not $SkipPublish) {
    Write-Host "`n[2/3] Publishing self-contained executable..." -ForegroundColor Yellow
    Stop-Process -Name PixelDogReminders -Force -ErrorAction SilentlyContinue
    
    dotnet publish "$RootDir\PixelDogReminders.csproj" `
        -c Release `
        -r win-x64 `
        --self-contained `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o "$RootDir\publish"
        
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed."
        exit $LASTEXITCODE
    }
} else {
    Write-Host "`n[2/3] Skipping publish step as requested." -ForegroundColor Gray
}

# 3. Find Inno Setup Compiler (ISCC.exe)
Write-Host "`n[3/3] Compiling Inno Setup installer..." -ForegroundColor Yellow

$IsccCandidates = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

$IsccPath = $null
foreach ($candidate in $IsccCandidates) {
    if (Test-Path $candidate) {
        $IsccPath = $candidate
        break
    }
}

if (-not $IsccPath) {
    $CommandIscc = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($CommandIscc) {
        $IsccPath = $CommandIscc.Source
    }
}

if (-not $IsccPath) {
    Write-Error "Could not find ISCC.exe (Inno Setup Compiler). Please ensure Inno Setup is installed."
    exit 1
}

Write-Host "Using Inno Setup Compiler: $IsccPath" -ForegroundColor Gray

# Create output folder
$InstallerOutputDir = "$RootDir\publish\installer"
if (-not (Test-Path $InstallerOutputDir)) {
    New-Item -ItemType Directory -Path $InstallerOutputDir -Force | Out-Null
}

# Compile installer
& $IsccPath "$RootDir\installer\setup.iss"

if ($LASTEXITCODE -eq 0) {
    $SetupExe = "$InstallerOutputDir\OyeDogSetup.exe"
    if (Test-Path $SetupExe) {
        $SizeMb = [math]::Round((Get-Item $SetupExe).Length / 1MB, 2)
        Write-Host "`n=========================================" -ForegroundColor Green
        Write-Host "🎉 SUCCESS! Single Installer Generated:" -ForegroundColor Green
        Write-Host "📦 $SetupExe ($SizeMb MB)" -ForegroundColor White
        Write-Host "=========================================" -ForegroundColor Green
    }
} else {
    Write-Error "Inno Setup compilation failed."
    exit $LASTEXITCODE
}
