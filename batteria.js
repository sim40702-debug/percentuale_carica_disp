// batteria.js
// Qui chiediamo a Windows la batteria dei dispositivi Bluetooth.
// Usiamo un piccolo comando PowerShell, poi leggiamo il risultato in JavaScript.

const { execFile } = require("child_process");

// Il "codice" con cui Windows salva la batteria dei dispositivi Bluetooth
const CHIAVE_BATTERIA = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";

// Il comando PowerShell, scritto su una riga sola (i pezzi sono separati da ;)
// 1. prende i dispositivi Bluetooth collegati
// 2. per ognuno chiede la batteria
// 3. restituisce una lista in formato JSON
const comando =
  "$lista = @(); " +
  "$dispositivi = Get-PnpDevice -PresentOnly | Where-Object { $_.InstanceId -like 'BTH*' -and $_.Status -eq 'OK' }; " +
  "foreach ($d in $dispositivi) { " +
  "$b = Get-PnpDeviceProperty -InstanceId $d.InstanceId -KeyName '" + CHIAVE_BATTERIA + "' -ErrorAction SilentlyContinue; " +
  "if ($b -and $b.Data -ne $null) { $lista += [PSCustomObject]@{ nome = $d.FriendlyName; batteria = [int]$b.Data } } " +
  "}; " +
  "ConvertTo-Json -InputObject $lista";

// Windows a volte aggiunge pezzi al nome (es. "Cuffie Hands-Free AG"): li togliamo
function pulisciNome(nome) {
  const pezziDaTogliere = [" Hands-Free AG", " Hands-Free", " Avrcp Transport", " Stereo", " A2DP SNK"];

  for (let i = 0; i < pezziDaTogliere.length; i++) {
    nome = nome.replace(pezziDaTogliere[i], "");
  }

  return nome.trim();
}

// Dati finti, per provare l'app su Mac o Linux (dove PowerShell non c'è)
function datiDiProva() {
  return [
    { nome: "Cuffie", batteria: 85 },
    { nome: "Tastiera", batteria: 40 },
    { nome: "Controller", batteria: 12 },
    { nome: "Mouse", batteria: 64 }
  ];
}

// Funzione principale: legge le batterie e poi chiama "quandoFinito" con la lista
function leggiBatterie(quandoFinito) {
  // Se non siamo su Windows usiamo i dati di prova
  if (process.platform !== "win32") {
    quandoFinito(datiDiProva());
    return;
  }

  const opzioni = { windowsHide: true, timeout: 20000 };

  execFile("powershell.exe", ["-NoProfile", "-NonInteractive", "-Command", comando], opzioni, function (errore, risultato) {
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

    // Puliamo i nomi e togliamo i doppioni (uno stesso dispositivo può comparire più volte)
    const lista = [];
    const nomiVisti = [];

    for (let i = 0; i < datiWindows.length; i++) {
      const nome = pulisciNome(datiWindows[i].nome);

      if (!nomiVisti.includes(nome)) {
        nomiVisti.push(nome);
        lista.push({ nome: nome, batteria: datiWindows[i].batteria });
      }
    }

    quandoFinito(lista);
  });
}

module.exports = { leggiBatterie };
