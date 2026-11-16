; installer.iss
; Il programma di installazione (fatto con Inno Setup). Lo crea GitHub da solo a ogni release.
; - installa l'app per l'utente (non servono i permessi da amministratore)
; - la mette nel menu Start e nella lista "App installate" di Windows
; - la disinstallazione toglie anche i dati e l'avvio con Windows

#define Versione GetEnv("VERSIONE")

[Setup]
AppId={{9C4A7E2B-5B1D-4F7A-9E3C-2B8D6A1F0C55}
AppName=Batterie in Barra
AppVersion={#Versione}
AppVerName=Batterie in Barra {#Versione}
AppPublisher=Simone
AppPublisherURL=https://github.com/sim40702-debug/percentuale_carica_disp
DefaultDirName={localappdata}\Programs\BatterieInBarra
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=uscita-installer
OutputBaseFilename=BatterieInBarra-Setup-{#Versione}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\BatterieInBarra.exe
UninstallDisplayName=Batterie in Barra
; Se l'app è aperta, l'installer chiede di chiuderla
AppMutex=BatterieInBarra_Simone
CloseApplications=yes

[Languages]
Name: "it"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"

[Files]
Source: "uscita\BatterieInBarra.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Il collegamento nel menu Start (con lo stesso nome usato dalle notifiche)
Name: "{autoprograms}\Batterie in Barra"; Filename: "{app}\BatterieInBarra.exe"; AppUserModelID: "Simone.BatterieInBarra"

[Run]
; Alla fine dell'installazione: "Avvia Batterie in Barra"
Filename: "{app}\BatterieInBarra.exe"; Description: "{cm:LaunchProgram,Batterie in Barra}"; Flags: nowait postinstall skipifsilent

[Registry]
; Quando disinstalli, togliamo anche l'avvio con Windows
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "BatterieInBarra"; Flags: uninsdeletevalue dontcreatekey

[UninstallDelete]
; Quando disinstalli, togliamo anche i dati (impostazioni, cronologia, statistiche, registro)
Type: filesandordirs; Name: "{userappdata}\BatterieInBarra"
Type: files; Name: "{app}\BatterieInBarra.vecchio.exe"
Type: files; Name: "{app}\BatterieInBarra.nuovo.exe"
