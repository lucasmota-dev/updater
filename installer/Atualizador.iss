#define AppName "Atualizador Windows"
#ifndef AppVersion
  #define AppVersion "0.0.0.0"
#endif
#define AppPublisher "Atualizador Windows"
#define AppExeName "Atualizador.Windows.exe"
#define PublishDir "..\artifacts\publish\win-x64"
#define IconFile "..\assets\icons\Atualizador.ico"

[Setup]
AppId={{B7B9E7A4-3A09-4DB1-9F5D-0F994D700A01}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
; Instalação por usuário (%LocalAppData%\Programs\Atualizador), sem UAC: permite a atualização automática silenciosa.
DefaultDirName={autopf}\Atualizador
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
CloseApplications=force
OutputDir=..\artifacts
OutputBaseFilename=Atualizador-Setup
SetupIconFile={#IconFile}
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion={#AppVersion}
VersionInfoProductName={#AppName}
VersionInfoDescription=Instalador do Atualizador Windows

[Files]
Source: "{#PublishDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; DestName: "README.txt"; Flags: ignoreversion

[Icons]
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#AppExeName}"
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#AppExeName}"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Executar {#AppName}"; Flags: nowait postinstall skipifsilent
; Atualização automática (/AUTOUPDATE=1, vinda do próprio aplicativo): reabre o aplicativo ao terminar.
Filename: "{app}\{#AppExeName}"; Flags: nowait; Check: IsAutoUpdate

[Code]
function IsAutoUpdate: Boolean;
begin
  Result := ExpandConstant('{param:AUTOUPDATE|0}') = '1';
end;
