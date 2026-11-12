// posizione.js
// Controlla dove si trova la barra di Windows e dove iniziano le icone vicino all'orologio,
// così sappiamo dove mettere le mini batterie.
// Controlla anche se c'è un gioco o un video a schermo intero (in quel caso le nascondiamo).

const { spawn } = require("child_process");
const { screen } = require("electron");

// Questo PowerShell resta acceso e ogni mezzo secondo controlla la barra.
// Scrive una riga solo quando qualcosa cambia.
// (usa alcune funzioni di Windows scritte in C#: FindWindow, GetWindowRect...)
const script = `
$idElectron = ID_ELECTRON
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Text;
using System.Runtime.InteropServices;
public class Finestre {
  [StructLayout(LayoutKind.Sequential)]
  public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern IntPtr FindWindow(string classe, string titolo);
  [DllImport("user32.dll")] public static extern IntPtr FindWindowEx(IntPtr padre, IntPtr dopo, string classe, string titolo);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr finestra, out RECT r);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern int GetClassName(IntPtr finestra, StringBuilder testo, int massimo);
}
"@
[Finestre]::SetProcessDPIAware() | Out-Null
$ultimaRiga = ""

while ($true) {
  # Se l'app si è chiusa, chiudiamo anche questo controllo
  if (-not (Get-Process -Id $idElectron -ErrorAction SilentlyContinue)) { exit }

  # La barra di Windows
  $barra = [Finestre]::FindWindow("Shell_TrayWnd", $null)
  $r = New-Object Finestre+RECT
  [Finestre]::GetWindowRect($barra, [ref]$r) | Out-Null

  # Dove iniziano le icone vicino all'orologio
  $icone = [Finestre]::FindWindowEx($barra, [IntPtr]::Zero, "TrayNotifyWnd", $null)
  $inizioIcone = -1
  if ($icone -ne [IntPtr]::Zero) {
    $ri = New-Object Finestre+RECT
    [Finestre]::GetWindowRect($icone, [ref]$ri) | Out-Null
    $inizioIcone = $ri.Left
  }

  # C'è qualcosa a schermo intero? (gioco, video...)
  $davanti = [Finestre]::GetForegroundWindow()
  $rd = New-Object Finestre+RECT
  [Finestre]::GetWindowRect($davanti, [ref]$rd) | Out-Null
  $classe = New-Object System.Text.StringBuilder 256
  [Finestre]::GetClassName($davanti, $classe, 256) | Out-Null
  $schermo = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
  $intero = ($rd.Left -le $schermo.Left) -and ($rd.Top -le $schermo.Top) -and ($rd.Right -ge $schermo.Right) -and ($rd.Bottom -ge $schermo.Bottom)
  if ($classe.ToString() -eq "Progman" -or $classe.ToString() -eq "WorkerW") { $intero = $false }

  $riga = '{"sinistra":' + $r.Left + ',"alto":' + $r.Top + ',"destra":' + $r.Right + ',"basso":' + $r.Bottom + ',"icone":' + $inizioIcone + ',"schermoIntero":' + $intero.ToString().ToLower() + '}'
  if ($riga -ne $ultimaRiga) {
    [Console]::Out.WriteLine($riga)
    [Console]::Out.Flush()
    $ultimaRiga = $riga
  }

  Start-Sleep -Milliseconds 500
}
`;

// Windows ci dà i pixel "veri" dello schermo, Electron usa pixel "con lo zoom": convertiamo
function converti(dati) {
  const barra = screen.screenToDipRect(null, {
    x: dati.sinistra,
    y: dati.alto,
    width: dati.destra - dati.sinistra,
    height: dati.basso - dati.alto
  });

  // Se non troviamo le icone, facciamo una stima: 260 pixel prima del bordo destro
  let inizioIcone = barra.x + barra.width - 260;
  if (dati.icone > dati.sinistra) {
    inizioIcone = screen.screenToDipPoint({ x: dati.icone, y: dati.alto }).x;
  }

  return {
    alto: barra.y,
    altezza: barra.height,
    inizioIcone: inizioIcone,
    schermoIntero: dati.schermoIntero
  };
}

// Avvia il controllo. Ogni volta che qualcosa cambia chiama "quandoCambia"
function avviaControlloBarra(quandoCambia) {
  if (process.platform !== "win32") {
    return null;
  }

  const testo = script.replace("ID_ELECTRON", String(process.pid));
  const codificato = Buffer.from(testo, "utf16le").toString("base64");
  const processo = spawn("powershell.exe", ["-NoProfile", "-NonInteractive", "-EncodedCommand", codificato], { windowsHide: true });

  // Leggiamo quello che scrive PowerShell, una riga alla volta
  let pezzoRimasto = "";
  processo.stdout.on("data", function (pezzo) {
    pezzoRimasto = pezzoRimasto + pezzo.toString();
    const righe = pezzoRimasto.split("\n");
    pezzoRimasto = righe.pop();   // l'ultima riga potrebbe non essere finita

    for (let i = 0; i < righe.length; i++) {
      const riga = righe[i].trim();
      if (riga.startsWith("{")) {
        try {
          quandoCambia(converti(JSON.parse(riga)));
        } catch (e) {
          console.log("Riga non valida:", riga);
        }
      }
    }
  });

  return processo;
}

module.exports = { avviaControlloBarra };
