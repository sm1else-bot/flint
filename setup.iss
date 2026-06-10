#define AppName "Flint"
#define AppVersion "1.0"
#define AppPublisher "Flint"
#define AppExeName "Flint.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir=installer
OutputBaseFilename=FlintSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"

[Files]
Source: "publish\Flint.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\WebView2Loader.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\D3DCompiler_47_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\PenImc_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\PresentationNative_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\vcruntime140_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\wpfgfx_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch Flint"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\Flint\WebView2"
