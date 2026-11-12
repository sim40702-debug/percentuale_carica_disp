// main.js
// Il "cervello" dell'app: mette le mini batterie direttamente sulla barra di Windows
// (a sinistra delle icone vicino all'orologio), le aggiorna e apre il widget al clic.

const { app, BrowserWindow, Menu, Notification, screen, ipcMain } = require("electron");
const path = require("path");
const fs = require("fs");
const { leggiBatterie } = require("./batteria");
const { avviaControlloBarra } = require("./posizione");

// ----- Impostazioni -----
const OGNI_QUANTO = 30 * 1000;      // aggiorna le batterie ogni 30 secondi
const SOGLIA_BASSA = 20;            // sotto questa percentuale arriva l'avviso
const LARGHEZZA_WIDGET = 340;
const LARGHEZZA_DISPOSITIVO = 90;   // spazio sulla barra per ogni dispositivo (uguale al CSS)
const LARGHEZZA_VUOTA = 44;         // spazio quando non c'è nessun dispositivo

// ----- Variabili -----
let barra = null;            // la finestrella appoggiata sulla barra di Windows
let widget = null;           // il widget grande stile Apple
let processoBarra = null;    // il PowerShell che controlla dove si trova la barra
let infoBarra = null;        // posizione della barra di Windows
let ultimiDati = [];         // l'ultima lista di batterie
let giaAvvisati = {};        // per non ripetere l'avviso batteria bassa
let ultimaChiusura = 0;      // quando il widget si è chiuso l'ultima volta

// File dove salviamo lo spostamento scelto con il menu
const fileImpostazioni = path.join(app.getPath("userData"), "impostazioni.json");
let spostamento = leggiSpostamento();

// Permettiamo una sola copia dell'app aperta
if (!app.requestSingleInstanceLock()) {
  app.quit();
}

// Serve a Windows per mostrare le notifiche con il nome giusto
app.setAppUserModelId("ch.simone.batterieinbarra");

app.whenReady().then(function () {
  creaBarra();
  creaWidget();

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

  // Avvio automatico con Windows (solo nella versione installata con il .exe)
  if (app.isPackaged) {
    app.setLoginItemSettings({ openAtLogin: true });
  }
});

// Quando l'app si chiude, chiudiamo anche il PowerShell
app.on("before-quit", function () {
  if (processoBarra !== null) {
    processoBarra.kill();
  }
});

app.on("window-all-closed", function () {});


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

// Mette la finestrella nel punto giusto della barra di Windows
function posizionaBarra() {
  if (barra === null) {
    return;
  }

  // Su Windows aspettiamo di sapere dove si trova la barra
  if (process.platform === "win32" && infoBarra === null) {
    return;
  }

  // Larghezza in base a quanti dispositivi ci sono
  let larghezza = LARGHEZZA_VUOTA;
  if (ultimiDati.length > 0) {
    larghezza = ultimiDati.length * LARGHEZZA_DISPOSITIVO + 4;
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
    x = infoBarra.inizioIcone - larghezza - 4 - spostamento;
    y = infoBarra.alto;
  } else {
    // Su Mac (prova): in basso a destra dello schermo
    const area = screen.getPrimaryDisplay().workArea;
    x = area.x + area.width - larghezza - 20 - spostamento;
    y = area.y + area.height - altezza - 10;
  }

  barra.setBounds({ x: Math.round(x), y: Math.round(y), width: Math.round(larghezza), height: Math.round(altezza) });

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
    { type: "separator" },
    { label: "Sposta a sinistra", click: function () { cambiaSpostamento(20); } },
    { label: "Sposta a destra", click: function () { cambiaSpostamento(-20); } },
    { type: "separator" },
    { label: "Esci", click: function () { app.quit(); } }
  ]);
  menu.popup();
});


// ----- Spostamento a mano (se le batterie coprono qualcosa) -----

function leggiSpostamento() {
  try {
    const testo = fs.readFileSync(fileImpostazioni, "utf8");
    return JSON.parse(testo).spostamento;
  } catch (e) {
    return 0;   // il file non c'è ancora
  }
}

function cambiaSpostamento(quanto) {
  spostamento = spostamento + quanto;
  fs.writeFileSync(fileImpostazioni, JSON.stringify({ spostamento: spostamento }));
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


// ----- Aggiornamento -----

function aggiorna() {
  leggiBatterie(function (lista) {
    ultimiDati = lista;

    // Mandiamo i dati alla barra e al widget
    const testo = JSON.stringify(lista);
    barra.webContents.executeJavaScript("mostraBarra(" + testo + ")");
    widget.webContents.executeJavaScript("mostraDati(" + testo + ")");

    posizionaBarra();
    controllaBatterieBasse(lista);
  });
}


// ----- Avviso batteria bassa -----

function controllaBatterieBasse(lista) {
  for (let i = 0; i < lista.length; i++) {
    const nome = lista[i].nome;
    const percentuale = lista[i].batteria;

    if (percentuale <= SOGLIA_BASSA && !giaAvvisati[nome]) {
      new Notification({
        title: "Batteria scarica",
        body: nome + " è al " + percentuale + "%. Mettilo in carica!"
      }).show();
      giaAvvisati[nome] = true;
    }

    // Quando torna sopra la soglia, l'avviso potrà arrivare di nuovo
    if (percentuale > SOGLIA_BASSA) {
      giaAvvisati[nome] = false;
    }
  }
}
