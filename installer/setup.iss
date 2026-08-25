; Inno Setup Script for Oye Dog (Pixel Dog Reminders)
; Per-user installation to %LOCALAPPDATA%\Programs\OyeDog (No admin UAC needed)

#define MyAppName "Oye Dog"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Abhishek's Companion"
#define MyAppExeName "PixelDogReminders.exe"

[Setup]
AppId={{D0917202-601D-46FE-9F5B-4C9031F8A6B1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\OyeDog
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\publish\installer
OutputBaseFilename=OyeDogSetup
SetupIconFile=app_icon.ico
UninstallDisplayIcon={app}\app_icon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=force

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "app_icon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app_icon.ico"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app_icon.ico"; Tasks: desktopicon

[Registry]
; Auto-launch on Windows login in Task Manager Startup Apps
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "OyeDog"; ValueData: """{app}\{#MyAppExeName}"" --startup"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
