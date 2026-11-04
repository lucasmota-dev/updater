; Instalador do App Updater (Inno Setup 7). Compilado pelo workflow .github/workflows/release.yml:
;   ISCC.exe /DAppVersion=yyyy.MM.dd.NN installer/Atualizador.iss
; Instala por usuário (sem UAC) em %LocalAppData%\Programs\Atualizador, o que permite
; ao próprio app executar este instalador em modo silencioso para se atualizar.

#define AppName "App Updater"
; Nome antigo do aplicativo: os atalhos com esse nome são removidos em [InstallDelete].
#define LegacyAppName "Atualizador Windows"
#ifndef AppVersion
  #define AppVersion "0.0.0.0"
#endif
#define AppPublisher "Lucas Mota"
#define AppUrl "https://github.com/lucasmota-dev/updater"
#define AppExeName "Atualizador.Windows.exe"
#define PublishDir "..\artifacts\publish\win-x64"
#define IconFile "..\assets\icons\Atualizador.ico"

[Setup]
; O AppId NÃO pode mudar: ele liga as versões novas à instalação existente e é lido pelo
; AppSelfUpdateService (chave ..._is1 em HKCU\...\Uninstall).
AppId={{B7B9E7A4-3A09-4DB1-9F5D-0F994D700A01}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
; A pasta continua "Atualizador" para as instalações existentes serem atualizadas no mesmo lugar.
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
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; Licença (MIT) e aviso de privacidade exibidos antes da instalação. A atualização silenciosa não mostra páginas.
LicenseFile=..\LICENSE
InfoBeforeFile=PRIVACIDADE.txt
VersionInfoVersion={#AppVersion}
; "Versão do produto" igual à versão do app: o SignPath confere esse campo antes de assinar.
VersionInfoProductVersion={#AppVersion}
VersionInfoProductTextVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoProductName={#AppName}
VersionInfoDescription=Instalador do {#AppName}

[Files]
Source: "{#PublishDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; DestName: "README.txt"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion
Source: "PRIVACIDADE.txt"; DestDir: "{app}"; Flags: ignoreversion

[InstallDelete]
; Atalhos com o nome antigo do aplicativo.
Type: files; Name: "{autodesktop}\{#LegacyAppName}.lnk"
Type: files; Name: "{group}\{#LegacyAppName}.lnk"

[Icons]
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#AppExeName}"
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#AppExeName}"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Executar o {#AppName}"; Flags: nowait postinstall skipifsilent
; Atualização automática (/AUTOUPDATE=1, enviado pelo próprio aplicativo): reabre o app ao terminar.
Filename: "{app}\{#AppExeName}"; Flags: nowait; Check: IsAutoUpdate

[Code]
function IsAutoUpdate: Boolean;
begin
  Result := ExpandConstant('{param:AUTOUPDATE|0}') = '1';
end;
