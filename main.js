// main.js
// Il "cervello" dell'app: mette le mini batterie vicino all'orologio,
// le aggiorna ogni minuto e apre il widget quando ci clicchi sopra.

const { app, Tray, Menu, BrowserWindow, nativeImage, Notification, screen } = require("electron");
const path = require("path");
const { leggiBatterie } = require("./batteria");

// ----- Impostazioni -----
const OGNI_QUANTO = 60 * 1000;   // aggiorna ogni 60 secondi
const SOGLIA_BASSA = 20;         // sotto questa percentuale arriva l'avviso
const LARGHEZZA_WIDGET = 340;

// ----- Variabili -----
let widget = null;          // la finestra del widget
let icone = {};             // le mini batterie nella barra (una per dispositivo)
let giaAvvisati = {};       // per non ripetere l'avviso batteria bassa ogni minuto
let ultimaChiusura = 0;     // quando il widget si è chiuso l'ultima volta

// Permettiamo una sola copia dell'app aperta
if (!app.requestSingleInstanceLock()) {
  app.quit();
}

// Serve a Windows per mostrare le notifiche con il nome giusto
app.setAppUserModelId("ch.simone.batterieinbarra");

app.whenReady().then(function () {
  creaWidget();

  // Quando il widget è pronto, facciamo il primo aggiornamento e poi uno ogni minuto
  widget.webContents.once("did-finish-load", function () {
    aggiorna();
    setInterval(aggiorna, OGNI_QUANTO);
  });

  // Avvio automatico con Windows (solo nella versione installata con il .exe)
  if (app.isPackaged) {
    app.setLoginItemSettings({ openAtLogin: true });
  }
});

// L'app non ha finestre normali: non deve chiudersi da sola
app.on("window-all-closed", function () {});


// ----- Il widget -----

function creaWidget() {
  widget = new BrowserWindow({
    width: LARGHEZZA_WIDGET,
    height: 300,
    show: false,          // all'inizio è nascosto
    frame: false,         // niente barra del titolo
    transparent: true,    // così si vedono gli angoli arrotondati
    resizable: false,
    skipTaskbar: true,    // non compare nella barra come una app normale
    alwaysOnTop: true
  });

  widget.loadFile(path.join(__dirname, "widget.html"));

  // Se clicchi fuori dal widget, si chiude
  widget.on("blur", function () {
    widget.hide();
    ultimaChiusura = Date.now();
  });
}

async function apriChiudiWidget(icona) {
  // Se si è appena chiuso (perché hai cliccato sull'icona), non lo riapriamo
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

  // Posizione: sopra l'icona cliccata, senza uscire dallo schermo
  const posIcona = icona.getBounds();
  const schermo = screen.getDisplayNearestPoint({ x: posIcona.x, y: posIcona.y }).workArea;

  let x = posIcona.x + posIcona.width / 2 - LARGHEZZA_WIDGET / 2;
  if (x + LARGHEZZA_WIDGET > schermo.x + schermo.width) {
    x = schermo.x + schermo.width - LARGHEZZA_WIDGET - 8;
  }
  if (x < schermo.x) {
    x = schermo.x + 8;
  }
  const y = schermo.y + schermo.height - altezzaFinestra - 4;

  widget.setBounds({ x: Math.round(x), y: Math.round(y), width: LARGHEZZA_WIDGET, height: altezzaFinestra });
  widget.show();
  widget.focus();
}


// ----- Aggiornamento -----

function aggiorna() {
  leggiBatterie(async function (lista) {
    await aggiornaIcone(lista);
    controllaBatterieBasse(lista);

    // Mandiamo i dati al widget
    widget.webContents.executeJavaScript("mostraDati(" + JSON.stringify(lista) + ")");
  });
}


// ----- Le mini batterie nella barra -----

// Il widget sa disegnare le mini batterie (con il canvas): gli chiediamo l'immagine
async function creaImmagine(percentuale) {
  const immagine = nativeImage.createEmpty();
  const misure = [
    { scala: 1, lato: 16 },
    { scala: 1.5, lato: 24 },
    { scala: 2, lato: 32 }
  ];

  // Una versione per ogni zoom dello schermo (100%, 150%, 200%)
  for (let i = 0; i < misure.length; i++) {
    const lato = misure[i].lato;
    const disegno = await widget.webContents.executeJavaScript("disegnaMiniBatteria(" + percentuale + ", " + lato + ")");
    immagine.addRepresentation({ scaleFactor: misure[i].scala, width: lato, height: lato, dataURL: disegno });
  }

  return immagine;
}

async function aggiornaIcone(lista) {
  // Se non c'è nessun dispositivo, mostriamo una batteria vuota con il "?"
  if (lista.length === 0) {
    lista = [{ nome: "Nessun dispositivo", batteria: -1 }];
  }

  // 1. Creiamo o aggiorniamo una icona per ogni dispositivo
  for (let i = 0; i < lista.length; i++) {
    const nome = lista[i].nome;
    const percentuale = lista[i].batteria;
    const immagine = await creaImmagine(percentuale);

    if (icone[nome] === undefined) {
      const nuova = new Tray(immagine);
      nuova.on("click", function () {
        apriChiudiWidget(nuova);
      });
      nuova.on("right-click", function () {
        nuova.popUpContextMenu(creaMenu());
      });
      icone[nome] = nuova;
    } else {
      icone[nome].setImage(immagine);
    }

    if (percentuale < 0) {
      icone[nome].setToolTip("Nessun dispositivo con batteria");
    } else {
      icone[nome].setToolTip(nome + ": " + percentuale + "%");
    }
  }

  // 2. Togliamo le icone dei dispositivi che non ci sono più
  const nomiAttuali = [];
  for (let i = 0; i < lista.length; i++) {
    nomiAttuali.push(lista[i].nome);
  }

  for (const nome in icone) {
    if (!nomiAttuali.includes(nome)) {
      icone[nome].destroy();
      delete icone[nome];
    }
  }
}

// Menu con il tasto destro sull'icona
function creaMenu() {
  return Menu.buildFromTemplate([
    { label: "Aggiorna ora", click: aggiorna },
    { type: "separator" },
    { label: "Esci", click: function () { app.quit(); } }
  ]);
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
