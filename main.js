// main.js
// Il "cervello" dell'app: mette le mini batterie direttamente sulla barra di Windows
// (a sinistra delle icone vicino all'orologio), le aggiorna, apre il widget e le impostazioni.

const { app, BrowserWindow, Menu, Notification, screen, ipcMain } = require("electron");
const path = require("path");
const { leggiBatterie } = require("./batteria");
const { avviaControlloBarra } = require("./posizione");
const { leggiImpostazioni, salvaImpostazioni } = require("./impostazioni");
const { aggiungiLettura, ultime24Ore, tempoStimato } = require("./cronologia");

// ----- Costanti -----
const OGNI_QUANTO = 30 * 1000;      // aggiorna le batterie ogni 30 secondi
const LARGHEZZA_WIDGET = 340;
const LARGHEZZA_CON_PERCENTUALE = 90;   // spazio per dispositivo (uguale al CSS + margini)
const LARGHEZZA_SENZA_PERCENTUALE = 64;
const LARGHEZZA_VUOTA = 44;         // quando non c'è nessun dispositivo
const DURATA_DISSOLVENZA = 350;     // millisecondi (uguale al CSS)

// ----- Variabili -----
let barra = null;                // la finestrella appoggiata sulla barra di Windows
let widget = null;               // il widget grande stile Apple
let finestraImpostazioni = null; // la finestra Impostazioni
let processoBarra = null;        // il PowerShell che controlla dove si trova la barra
let infoBarra = null;            // posizione della barra di Windows
let larghezzaBarra = LARGHEZZA_VUOTA;
let ultimiDati = [];             // l'ultima lista letta da Windows (nomi originali)
let ultimiValori = {};           // l'ultima % di ogni dispositivo (per l'avviso carica completa)
let giaAvvisati = {};            // per non ripetere l'avviso batteria scarica
let ultimaChiusura = 0;          // quando il widget si è chiuso l'ultima volta

let impostazioni = leggiImpostazioni();

// Permettiamo una sola copia dell'app aperta
if (!app.requestSingleInstanceLock()) {
  app.quit();
}

// Serve a Windows per mostrare le notifiche con il nome giusto
app.setAppUserModelId("ch.simone.batterieinbarra");

app.whenReady().then(function () {
  creaBarra();
  creaWidget();
  applicaAvvioWindows();

  // Quando la barra è pronta: primo aggiornamento, poi ogni 30 secondi
  barra.webContents.once("did-finish-load", function () {
    aggiorna();
    setInterval(aggiorna, OGNI_QUANTO);
  });

  // Controlliamo dove si trova la barra di Windows
  processoBarra = avviaControlloBarra(function (info) {
    infoBarra = info;
    posizionaBarra();
  });

  // Quando clicchi sulla barra di Windows, lei passa davanti alla nostra:
  // ogni secondo rimettiamo le batterie davanti
  setInterval(function () {
    if (barra.isVisible()) {
      barra.moveTop();
    }
  }, 1000);
});

// Quando l'app si chiude, chiudiamo anche il PowerShell
app.on("before-quit", function () {
  if (processoBarra !== null) {
    processoBarra.kill();
  }
});

app.on("window-all-closed", function () {});


// ----- Aggiornamento -----

function aggiorna() {
  leggiBatterie(function (lista) {
    ultimiDati = lista;

    for (let i = 0; i < lista.length; i++) {
      const nome = lista[i].nome;
      const percentuale = lista[i].batteria;

      ricordaDispositivo(nome);
      aggiungiLettura(nome, percentuale);
      controllaAvvisi(nome, percentuale);
    }

    mostraTutto();
  });
}

// Salviamo ogni dispositivo visto, così compare nelle Impostazioni anche quando è spento
function ricordaDispositivo(nome) {
  if (!impostazioni.visti.includes(nome)) {
    impostazioni.visti.push(nome);
    salvaImpostazioni(impostazioni);
  }
}

// Prepara i dati da mostrare (con i nomi scelti da te, il tempo e la cronologia)
function creaVista() {
  const vista = [];

  for (let i = 0; i < ultimiDati.length; i++) {
    const originale = ultimiDati[i].nome;

    vista.push({
      originale: originale,
      nome: impostazioni.nomi[originale] || originale,
      batteria: ultimiDati[i].batteria,
      sullaBarra: !impostazioni.nascosti.includes(originale),
      tempo: tempoStimato(originale),
      storia: ultime24Ore(originale)
    });
  }
  return vista;
}

// Manda i dati alla barra e al widget
function mostraTutto() {
  const vista = creaVista();

  // Sulla barra solo quelli non nascosti
  const perLaBarra = [];
  for (let i = 0; i < vista.length; i++) {
    if (vista[i].sullaBarra) {
      perLaBarra.push(vista[i]);
    }
  }

  const opzioni = {
    mostraPercentuale: impostazioni.mostraPercentuale,
    animazioni: impostazioni.animazioni
  };

  barra.webContents.executeJavaScript("mostraBarra(" + JSON.stringify(perLaBarra) + ", " + JSON.stringify(opzioni) + ")");
  widget.webContents.executeJavaScript("mostraDati(" + JSON.stringify(vista) + ")");

  aggiornaLarghezza(perLaBarra.length);
}


// ----- Avvisi -----

function controllaAvvisi(originale, percentuale) {
  const nome = impostazioni.nomi[originale] || originale;

  // Batteria scarica
  if (percentuale <= impostazioni.soglia && !giaAvvisati[originale]) {
    mandaNotifica("Batteria scarica", nome + " è al " + percentuale + "%. Mettilo in carica!");
    giaAvvisati[originale] = true;
  }
  // Quando torna sopra la soglia, l'avviso potrà arrivare di nuovo
  if (percentuale > impostazioni.soglia) {
    giaAvvisati[originale] = false;
  }

  // Carica completa: prima era sotto 100, adesso è 100
  const prima = ultimiValori[originale];
  if (impostazioni.avvisoCompleta && prima !== undefined && prima < 100 && percentuale === 100) {
    mandaNotifica("Carica completa", nome + " è carico al 100%. Puoi staccarlo!");
  }
  ultimiValori[originale] = percentuale;
}

function mandaNotifica(titolo, testo) {
  new Notification({ title: titolo, body: testo }).show();
}


// ----- La finestrella sulla barra -----

function creaBarra() {
  barra = new BrowserWindow({
    width: LARGHEZZA_VUOTA,
    height: 48,
    show: false,
    frame: false,
    transparent: true,
    resizable: false,
    movable: false,
    skipTaskbar: true,
    focusable: false,      // non "ruba" il clic alle altre finestre
    hasShadow: false,
    webPreferences: {
      preload: path.join(__dirname, "preload.js")
    }
  });

  barra.setAlwaysOnTop(true, "screen-saver");
  barra.loadFile(path.join(__dirname, "barra.html"));
}

// Calcola la larghezza giusta in base a quanti dispositivi ci sono
function calcolaLarghezza(quanti) {
  if (quanti === 0) {
    return LARGHEZZA_VUOTA;
  }
  if (impostazioni.mostraPercentuale) {
    return quanti * LARGHEZZA_CON_PERCENTUALE + 4;
  }
  return quanti * LARGHEZZA_SENZA_PERCENTUALE + 4;
}

function aggiornaLarghezza(quanti) {
  const nuova = calcolaLarghezza(quanti);

  // Se la barra si stringe, aspettiamo la fine della dissolvenza
  if (nuova < larghezzaBarra && impostazioni.animazioni) {
    setTimeout(function () {
      larghezzaBarra = nuova;
      posizionaBarra();
    }, DURATA_DISSOLVENZA);
  } else {
    larghezzaBarra = nuova;
    posizionaBarra();
  }
}

// Mette la finestrella nel punto giusto della barra di Windows
function posizionaBarra() {
  if (barra === null) {
    return;
  }

  // Su Windows aspettiamo di sapere dove si trova la barra
  if (process.platform === "win32" && infoBarra === null) {
    return;
  }

  let x = 0;
  let y = 0;
  let altezza = 48;

  if (infoBarra !== null) {
    // Su Windows: se c'è un gioco o un video a schermo intero, nascondiamo tutto
    if (infoBarra.schermoIntero) {
      barra.hide();
      return;
    }
    altezza = infoBarra.altezza;
    x = infoBarra.inizioIcone - larghezzaBarra - 4 - impostazioni.spostamento;
    y = infoBarra.alto;
  } else {
    // Su Mac (prova): in basso a destra dello schermo
    const area = screen.getPrimaryDisplay().workArea;
    x = area.x + area.width - larghezzaBarra - 20 - impostazioni.spostamento;
    y = area.y + area.height - altezza - 10;
  }

  barra.setBounds({ x: Math.round(x), y: Math.round(y), width: Math.round(larghezzaBarra), height: Math.round(altezza) });

  if (!barra.isVisible()) {
    barra.showInactive();   // la mostra senza togliere il focus alle altre finestre
  }
}

// Clic sinistro sulla barra
ipcMain.on("clic-barra", function () {
  apriChiudiWidget();
});

// Clic destro sulla barra: menu
ipcMain.on("menu-barra", function () {
  const menu = Menu.buildFromTemplate([
    { label: "Aggiorna ora", click: aggiorna },
    { label: "Impostazioni…", click: apriImpostazioni },
    { type: "separator" },
    { label: "Sposta a sinistra", click: function () { cambiaSpostamento(20); } },
    { label: "Sposta a destra", click: function () { cambiaSpostamento(-20); } },
    { type: "separator" },
    { label: "Esci", click: function () { app.quit(); } }
  ]);
  menu.popup();
});

function cambiaSpostamento(quanto) {
  impostazioni.spostamento = impostazioni.spostamento + quanto;
  salvaImpostazioni(impostazioni);
  posizionaBarra();
}


// ----- Il widget grande -----

function creaWidget() {
  widget = new BrowserWindow({
    width: LARGHEZZA_WIDGET,
    height: 300,
    show: false,
    frame: false,
    transparent: true,
    resizable: false,
    skipTaskbar: true,
    alwaysOnTop: true
  });

  widget.loadFile(path.join(__dirname, "widget.html"));

  // Se clicchi fuori dal widget, si chiude
  widget.on("blur", function () {
    widget.hide();
    ultimaChiusura = Date.now();
  });
}

async function apriChiudiWidget() {
  // Se si è appena chiuso, non lo riapriamo subito
  if (Date.now() - ultimaChiusura < 300) {
    return;
  }

  if (widget.isVisible()) {
    widget.hide();
    return;
  }

  // Chiediamo al widget quanto è alto il suo contenuto
  const altezza = await widget.webContents.executeJavaScript("document.getElementById('scheda').offsetHeight");
  const altezzaFinestra = altezza + 20;

  // Posizione: sopra le batterie, senza uscire dallo schermo
  const posBarra = barra.getBounds();
  const schermo = screen.getDisplayMatching(posBarra).bounds;

  let x = posBarra.x + posBarra.width - LARGHEZZA_WIDGET + 10;
  if (x + LARGHEZZA_WIDGET > schermo.x + schermo.width) {
    x = schermo.x + schermo.width - LARGHEZZA_WIDGET - 8;
  }
  if (x < schermo.x) {
    x = schermo.x + 8;
  }

  // Se la barra di Windows è in basso il widget va sopra, se è in alto va sotto
  let y = posBarra.y - altezzaFinestra;
  if (posBarra.y < schermo.y + schermo.height / 2) {
    y = posBarra.y + posBarra.height;
  }

  widget.setBounds({ x: Math.round(x), y: Math.round(y), width: LARGHEZZA_WIDGET, height: altezzaFinestra });
  widget.show();
  widget.focus();
}


// ----- La finestra Impostazioni -----

function apriImpostazioni() {
  // Se è già aperta, la portiamo davanti
  if (finestraImpostazioni !== null) {
    finestraImpostazioni.show();
    finestraImpostazioni.focus();
    return;
  }

  finestraImpostazioni = new BrowserWindow({
    width: 560,
    height: 740,
    useContentSize: true,   // le misure valgono per il contenuto, senza la barra del titolo
    title: "Batterie in Barra - Impostazioni",
    backgroundColor: "#1e1e20",
    resizable: false,
    maximizable: false,
    autoHideMenuBar: true,
    webPreferences: {
      preload: path.join(__dirname, "preload.js")
    }
  });

  finestraImpostazioni.loadFile(path.join(__dirname, "impostazioni.html"));

  finestraImpostazioni.on("closed", function () {
    finestraImpostazioni = null;
  });
}

// La pagina Impostazioni chiede i dati
ipcMain.handle("leggi-impostazioni", function () {
  const dispositivi = [];

  for (let i = 0; i < impostazioni.visti.length; i++) {
    const originale = impostazioni.visti[i];

    // È collegato adesso? Se sì, prendiamo anche la batteria
    let batteria = -1;
    for (let j = 0; j < ultimiDati.length; j++) {
      if (ultimiDati[j].nome === originale) {
        batteria = ultimiDati[j].batteria;
      }
    }

    dispositivi.push({
      originale: originale,
      nome: impostazioni.nomi[originale] || "",
      sullaBarra: !impostazioni.nascosti.includes(originale),
      batteria: batteria
    });
  }

  return { impostazioni: impostazioni, dispositivi: dispositivi };
});

// La pagina Impostazioni cambia un'opzione
ipcMain.on("cambia-impostazione", function (evento, chiave, valore) {
  const chiaviPermesse = ["mostraPercentuale", "animazioni", "soglia", "avvisoCompleta", "avvioWindows"];
  if (!chiaviPermesse.includes(chiave)) {
    return;
  }

  impostazioni[chiave] = valore;
  salvaImpostazioni(impostazioni);

  if (chiave === "avvioWindows") {
    applicaAvvioWindows();
  }
  if (chiave === "soglia") {
    giaAvvisati = {};   // con la nuova soglia gli avvisi ripartono da zero
  }
  mostraTutto();
});

// La pagina Impostazioni cambia il nome di un dispositivo
ipcMain.on("rinomina", function (evento, originale, nome) {
  nome = nome.trim();

  if (nome === "" || nome === originale) {
    delete impostazioni.nomi[originale];   // nome vuoto = torna al nome originale
  } else {
    impostazioni.nomi[originale] = nome;
  }

  salvaImpostazioni(impostazioni);
  mostraTutto();
});

// La pagina Impostazioni mostra o nasconde un dispositivo sulla barra
ipcMain.on("sulla-barra", function (evento, originale, mostra) {
  // Prima lo togliamo dalla lista dei nascosti...
  const nuovaLista = [];
  for (let i = 0; i < impostazioni.nascosti.length; i++) {
    if (impostazioni.nascosti[i] !== originale) {
      nuovaLista.push(impostazioni.nascosti[i]);
    }
  }
  // ...poi lo rimettiamo se va nascosto
  if (!mostra) {
    nuovaLista.push(originale);
  }

  impostazioni.nascosti = nuovaLista;
  salvaImpostazioni(impostazioni);
  mostraTutto();
});


// ----- Avvio con Windows -----

function applicaAvvioWindows() {
  // Solo nella versione installata con il .exe
  if (app.isPackaged) {
    app.setLoginItemSettings({ openAtLogin: impostazioni.avvioWindows });
  }
}
