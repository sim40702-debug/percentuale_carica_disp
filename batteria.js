// batteria.js
// Qui chiediamo a Windows la batteria dei dispositivi Bluetooth COLLEGATI.
// Usiamo un comando PowerShell, poi leggiamo il risultato in JavaScript.

const { execFile } = require("child_process");

// Il comando PowerShell (scritto su più righe per leggerlo meglio).
// 1. trova i dispositivi Bluetooth COLLEGATI adesso e si salva il loro indirizzo (MAC)
// 2. cerca la batteria, ma solo per quei dispositivi
// 3. restituisce una lista in formato JSON
const comando = `
$CHIAVE_COLLEGATO = '{83DA6326-97A6-4088-9453-A1923F573B29} 15'
$CHIAVE_BATTERIA = '{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2'

# 1. Dispositivi collegati: indirizzo -> nome
$collegati = @{}
foreach ($d in Get-PnpDevice -Class Bluetooth) {
  if ($d.InstanceId -match 'DEV_([0-9A-F]{12})') {
    $indirizzo = $Matches[1]
    $stato = (Get-PnpDeviceProperty -InstanceId $d.InstanceId -KeyName $CHIAVE_COLLEGATO -ErrorAction SilentlyContinue).Data
    if ($stato -eq $true) {
      $collegati[$indirizzo] = $d.FriendlyName
    }
  }
}

# 2. Batteria dei dispositivi collegati
$lista = @()
if ($collegati.Count -gt 0) {
  foreach ($d in Get-PnpDevice | Where-Object { $_.InstanceId -like 'BTH*' }) {
    $batteria = (Get-PnpDeviceProperty -InstanceId $d.InstanceId -KeyName $CHIAVE_BATTERIA -ErrorAction SilentlyContinue).Data
    if ($batteria -ne $null) {
      foreach ($indirizzo in $collegati.Keys) {
        if ($d.InstanceId -like "*$indirizzo*") {
          $lista += [PSCustomObject]@{ nome = $collegati[$indirizzo]; batteria = [int]$batteria }
        }
      }
    }
  }
}

# 3. Risultato
ConvertTo-Json -InputObject $lista
`;

// Dati finti, per provare l'app su Mac (dove PowerShell non c'è)
function datiDiProva() {
  return [
    { nome: "Cuffie", batteria: 85 },
    { nome: "Tastiera", batteria: 40 },
    { nome: "Controller", batteria: 12 }
  ];
}

// Funzione principale: legge le batterie e poi chiama "quandoFinito" con la lista
function leggiBatterie(quandoFinito) {
  // Se non siamo su Windows usiamo i dati di prova
  if (process.platform !== "win32") {
    quandoFinito(datiDiProva());
    return;
  }

  // PowerShell vuole il comando "codificato" quando è su più righe
  const codificato = Buffer.from(comando, "utf16le").toString("base64");
  const opzioni = { windowsHide: true, timeout: 30000 };

  execFile("powershell.exe", ["-NoProfile", "-NonInteractive", "-EncodedCommand", codificato], opzioni, function (errore, risultato) {
    if (errore) {
      console.log("Errore PowerShell:", errore.message);
      quandoFinito([]);
      return;
    }

    let datiWindows = [];
    try {
      datiWindows = JSON.parse(risultato);
    } catch (e) {
      console.log("Risposta non valida:", risultato);
    }

    // Uno stesso dispositivo può comparire più volte: teniamo solo il primo
    const lista = [];
    const nomiVisti = [];

    for (let i = 0; i < datiWindows.length; i++) {
      const nome = datiWindows[i].nome;

      if (!nomiVisti.includes(nome)) {
        nomiVisti.push(nome);
        lista.push({ nome: nome, batteria: datiWindows[i].batteria });
      }
    }

    quandoFinito(lista);
  });
}

module.exports = { leggiBatterie };
