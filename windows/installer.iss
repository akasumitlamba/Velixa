[Setup]
AppId={{ACE98A5E-9CB1-41C0-B507-6249D0E142E8}
AppName=Velixa
AppVersion=1.0.1
AppPublisher=Velixa
DefaultDirName={autopf}\Velixa
DefaultGroupName=Velixa
OutputDir=..\dist
OutputBaseFilename=Velixa-1.0.1-Windows-Setup
SetupIconFile=..\build\windows\velixa.ico
UninstallDisplayIcon={app}\Velixa.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
DisableProgramGroupPage=yes
InfoBeforeFile=..\assets\VB-CABLE-NOTICE.txt
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked
Name: "startup"; Description: "Open Velixa when I sign in to Windows"; Flags: checkedonce
Name: "vbcable"; Description: "Set up VB-CABLE by VB-Audio for shared microphones (donationware)"; Flags: unchecked; Check: NeedsVBCable
[Files]
Source: "..\build\windows\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\build\windows\Velixa.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\build\windows\Velixa.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\build\windows\velixa.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\build\windows\velixa-logo.png"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\build\windows\BouncyCastle.Cryptography.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\build\windows\QRCoder.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\build\windows\THIRD-PARTY-NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\build\windows\Velixa.Touchpad.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\build\windows\VB-CABLE\*"; DestDir: "{app}\VB-CABLE"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\Velixa"; Filename: "{app}\Velixa.exe"
Name: "{autodesktop}\Velixa"; Filename: "{app}\Velixa.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\VB-CABLE\VBCABLE_Setup_x64.exe"; WorkingDir: "{app}\VB-CABLE"; Tasks: vbcable; Flags: waituntilterminated skipifsilent; Check: NeedsVBCable

Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""Velixa local input"" dir=in action=allow program=""{app}\Velixa.exe"" protocol=TCP localport=37128 remoteip=LocalSubnet profile=private"; Flags: runhidden waituntilterminated
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""Velixa local discovery"" dir=in action=allow program=""{app}\Velixa.exe"" protocol=UDP localport=37129 remoteip=LocalSubnet profile=private"; Flags: runhidden waituntilterminated
Filename: "{app}\Velixa.exe"; Parameters: "--enable-startup"; Tasks: startup; Flags: runhidden waituntilterminated runasoriginaluser
Filename: "{app}\Velixa.exe"; Description: "Open Velixa"; Flags: nowait postinstall skipifsilent runasoriginaluser
[UninstallRun]
Filename: "{app}\Velixa.exe"; Parameters: "--disable-startup"; Flags: runhidden waituntilterminated; RunOnceId: "RemoveStartup"
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Velixa local input"" program=""{app}\Velixa.exe"""; Flags: runhidden; RunOnceId: "RemoveInputRule"
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Velixa local discovery"" program=""{app}\Velixa.exe"""; Flags: runhidden; RunOnceId: "RemoveDiscoveryRule"

[Code]
function NeedsVBCable: Boolean;
begin
  Result := not RegKeyExists(HKLM, 'SYSTEM\CurrentControlSet\Services\VBAudioVACMME');
end;
