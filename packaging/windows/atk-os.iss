; Windows installer for ATK-OS (Inno Setup 6).
; Build: iscc /DAppVersion=1.0.0 /DSourceDir=<publish dir> /DOutputDir=<dir> packaging\windows\atk-os.iss

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\..\out\atk-os-win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\..\out"
#endif

#define AppName "ATK-OS"
#define AppExe "atk-os.exe"
#define AppUrl "https://github.com/adilzhanIS7/atk-os"

[Setup]
AppId={{6E4D6BC1-3549-4F9E-96D3-A86449670672}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=Adilzhan
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
AppCopyright=Copyright (c) 2026 Adilzhan. MIT License.
VersionInfoVersion={#AppVersion}
VersionInfoDescription={#AppName} Setup

; Per-user install by default (no admin prompt); admins can pass /ALLUSERS.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

OutputDir={#OutputDir}
OutputBaseFilename=ATK-OS-Setup-{#AppVersion}-x64
SetupIconFile=..\..\src\AtkOs.App\Assets\icon.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
WizardStyle=modern
DisableWelcomePage=no
ShowLanguageDialog=auto
WizardImageFile=wizard.bmp,wizard-2x.bmp
WizardSmallImageFile=wizard-small.bmp,wizard-small-2x.bmp
LicenseFile=..\..\LICENSE
Compression=lzma2/ultra64
SolidCompression=yes

; The app sits in the tray: close it before updating or removing files.
CloseApplications=force
RestartApplications=no

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"

[CustomMessages]
en.AutostartTask=Start {#AppName} when I sign in (in the tray)
ru.AutostartTask=Запускать {#AppName} при входе в систему (в трее)
en.LaunchApp=Launch {#AppName}
ru.LaunchApp=Запустить {#AppName}

[Tasks]
Name: "autostart"; Description: "{cm:AutostartTask}"
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Same value the app writes from its "Start with system" switch.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#AppName}"; ValueData: """{app}\{#AppExe}"" --tray"; Tasks: autostart
; Always remove it on uninstall, even if it was turned on from the app.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "{#AppName}"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/f /im {#AppExe}"; Flags: runhidden; RunOnceId: "StopApp"
