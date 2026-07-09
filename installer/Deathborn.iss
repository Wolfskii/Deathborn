; Deathborn Windows installer — build with Inno Setup 6 (ISCC.exe).
; From repo root: task client:installer

#ifndef MyAppVersion
  #define MyAppVersion "0.1.0"
#endif

#ifndef PublishDir
  #define PublishDir "..\client\publish\win-x64"
#endif

#define MyAppName "Deathborn"
#define MyAppPublisher "Wolfskii"
#define MyAppURL "https://deathborn.wolfskii.dev"
#define MyAppExeName "Deathborn.Client.exe"

[Setup]
AppId={{B4E8F2A1-9C3D-4E5F-A1B2-DEATHBORN2026}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\dist
OutputBaseFilename=Deathborn-{#MyAppVersion}-win-x64-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
WizardImageFile=assets\wizard_large.bmp
WizardSmallImageFile=assets\wizard_small.bmp
SetupIconFile=..\client\Deathborn.Client\Icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Summon a desktop shortcut (recommended if you enjoy clicking things)"; GroupDescription: "Additional shortcuts:"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "assets\run_frame_*.bmp"; DestDir: "{tmp}"; Flags: dontcopy solidbreak noencryption

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName} (and probably die)"; Flags: nowait postinstall skipifsilent

[Messages]
english.WelcomeLabel1=Welcome to the Deathborn installer
english.WelcomeLabel2=You are born to die. Only skill decides when.%n%nThis wizard will install [name/ver] on your machine. Permadeath applies in-game only — uninstall is still allowed.%n%nBuilt with love (and spite) by Wolfskii for friends brave enough to log in.
english.ClickNext=Click Next to stop stalling at the login screen.
english.SelectDirLabel3=Where should Deathborn loot your disk space?
english.SelectDirBrowseLabel=Browse for a folder with fewer skeletons than usual:
english.DiskSpaceGBLabel=at least [gb] GB (your loot bag is bigger than that, right?)
english.StatusCreateDirs=Creating directory...
english.StatusExtractFiles=Looting files from the installer...
english.StatusCreateIcons=Placing shortcuts where you will forget them...
english.StatusCreateIniEntries=Writing config runes...
english.StatusCreateRegistryEntries=Whispering to the registry...
english.StatusRegisterFiles=Registering install with Windows (no soul binding involved)...
english.StatusRunProgram=Running post-install ritual...
english.FinishedHeadingLabel=Installation complete — you respawned on the desktop
english.FinishedLabel=Deathborn is installed.%n%nTip: dying in-game is permanent. Closing the launcher is not.%n%nMay your first death be instructive.
english.FinishedLabelNoIcons=Deathborn is installed.%n%nNo shortcuts were created — you are playing hardcore installer mode.

[Code]
var
  RunAnimImage: TBitmapImage;
  RunAnimFrame: Integer;
  RunAnimStatus: TNewStaticText;
  RunStatusIndex: Integer;
  RunAnimTimer: TTimer;
  RunMessageTimer: TTimer;

const
  RunAnimIntervalMs = 175;
  RunMessageIntervalMs = 7000;

function GetRunStatusLine(Index: Integer): string;
begin
  case Index of
    0: Result := 'Teaching the swordsman where your SSD lives...';
    1: Result := 'Binding files to Program Files (reversible, unlike in-game)...';
    2: Result := 'Rolling install check... nat 20!';
    3: Result := 'Looting the publish folder — full-loot enabled...';
    4: Result := 'Sharpening pixels. Blunting excuses...';
    5: Result := 'Whispering to api.deathborn.wolfskii.dev...';
    6: Result := 'Loading permadeath disclaimer into RAM...';
    7: Result := 'Almost there. Do not alt+F4 — that is not a respawn.';
  else
    Result := 'Installing Deathborn...';
  end;
end;

procedure ExtractRunFrames;
var
  I: Integer;
  Name: string;
begin
  for I := 0 to 7 do
  begin
    Name := 'run_frame_' + Format('%.2d', [I]) + '.bmp';
    ExtractTemporaryFile(Name);
  end;
end;

procedure SetRunFrame(Frame: Integer);
var
  Path: string;
begin
  Path := ExpandConstant('{tmp}\run_frame_' + Format('%.2d', [Frame]) + '.bmp');
  if FileExists(Path) then
    RunAnimImage.Bitmap.LoadFromFile(Path);
end;

procedure RunAnimStep;
begin
  RunAnimFrame := (RunAnimFrame + 1) mod 8;
  SetRunFrame(RunAnimFrame);
end;

procedure RunAnimTimerTimer(Sender: TObject);
begin
  if RunAnimImage.Visible then
    RunAnimStep;
end;

procedure RunMessageTimerTimer(Sender: TObject);
begin
  if not RunAnimStatus.Visible then
    Exit;
  RunStatusIndex := (RunStatusIndex + 1) mod 8;
  RunAnimStatus.Caption := GetRunStatusLine(RunStatusIndex);
end;

procedure ShowRunAnim;
begin
  RunAnimFrame := 0;
  RunStatusIndex := 0;
  SetRunFrame(0);
  RunAnimStatus.Caption := GetRunStatusLine(0);
  RunAnimImage.Visible := True;
  RunAnimStatus.Visible := True;
  RunAnimTimer.Enabled := True;
  RunMessageTimer.Enabled := True;
end;

procedure HideRunAnim;
begin
  RunAnimTimer.Enabled := False;
  RunMessageTimer.Enabled := False;
  RunAnimImage.Visible := False;
  RunAnimStatus.Visible := False;
end;

procedure InitializeWizard;
begin
  ExtractRunFrames;

  RunAnimImage := TBitmapImage.Create(WizardForm);
  RunAnimImage.Parent := WizardForm;
  RunAnimImage.Left := ScaleX(220);
  RunAnimImage.Top := ScaleY(170);
  RunAnimImage.Width := ScaleX(160);
  RunAnimImage.Height := ScaleY(160);
  RunAnimImage.Stretch := True;
  RunAnimImage.Visible := False;

  RunAnimStatus := TNewStaticText.Create(WizardForm);
  RunAnimStatus.Parent := WizardForm;
  RunAnimStatus.Left := ScaleX(200);
  RunAnimStatus.Top := ScaleY(340);
  RunAnimStatus.Width := ScaleX(420);
  RunAnimStatus.Height := ScaleY(40);
  RunAnimStatus.AutoSize := False;
  RunAnimStatus.WordWrap := True;
  RunAnimStatus.Font.Style := [fsItalic];
  RunAnimStatus.Caption := '';
  RunAnimStatus.Visible := False;

  RunAnimTimer := TTimer.Create(WizardForm);
  RunAnimTimer.OnTimer := @RunAnimTimerTimer;
  RunAnimTimer.Interval := RunAnimIntervalMs;
  RunAnimTimer.Enabled := False;

  RunMessageTimer := TTimer.Create(WizardForm);
  RunMessageTimer.OnTimer := @RunMessageTimerTimer;
  RunMessageTimer.Interval := RunMessageIntervalMs;
  RunMessageTimer.Enabled := False;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if (CurPageID = wpPreparing) or (CurPageID = wpInstalling) then
    ShowRunAnim
  else
    HideRunAnim;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = wpWelcome then
    WizardForm.NextButton.Caption := 'Accept fate'
  else if CurPageID = wpSelectDir then
    WizardForm.NextButton.Caption := 'Loot this folder'
  else if CurPageID = wpSelectTasks then
    WizardForm.NextButton.Caption := 'Yes, shortcuts'
  else
    WizardForm.NextButton.Caption := SetupMessage(msgButtonNext);
end;

function BackButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  WizardForm.BackButton.Caption := 'Retreat (no shame)';
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    HideRunAnim;
end;
