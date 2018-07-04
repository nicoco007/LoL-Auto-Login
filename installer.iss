#define Name "LoL Auto Login"
#define BuildNumber GetEnv("BUILD_NUMBER")
#define Version "2.1.0." + BuildNumber
#define Publisher "nicoco007"
#define URL "https://www.nicoco007.com/lol-auto-login/"
#define ExeName "LoLAutoLogin.exe"
#define DesktopShortcutName "League of Legends"

#define NameNoSpaces StringChange(Name, " ", "")
        
[Setup]
AppId={{F79A7574-607D-45EA-9099-465C0EFC68A0}
AppName={#Name}
AppVersion={#Version}
AppVerName={#Name}
AppPublisher={#Publisher}
AppPublisherURL={#URL}
AppSupportURL={#URL}
AppUpdatesURL={#URL}
DefaultDirName=C:\Riot Games\League of Legends
DisableProgramGroupPage=yes
OutputBaseFilename={#NameNoSpaces}-v{#Version}
Compression=lzma
SolidCompression=yes
DirExistsWarning=no
OutputDir=publish
SetupIconFile=installer\icon.ico
UninstallDisplayIcon={app}\{#ExeName}
WizardSmallImageFile="installer\WizardSmallImageFile\*.bmp"

[Messages]
BeveledLabel=  {#Name} v{#Version} Build {#BuildNumber}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "LoL Auto Login\bin\x86\Release\LoLAutoLogin.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "LoL Auto Login\bin\x86\Release\AutoItX3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "LoL Auto Login\bin\x86\Release\concrt140.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "LoL Auto Login\bin\x86\Release\cvextern.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "LoL Auto Login\bin\x86\Release\msvcp140.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "LoL Auto Login\bin\x86\Release\vcruntime140.dll"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{commonprograms}\{#Name}"; Filename: "{app}\{#ExeName}"
Name: "{commondesktop}\{#DesktopShortcutName}"; Filename: "{app}\{#ExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#ExeName}"; Description: "{cm:LaunchProgram,{#StringChange(Name, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

