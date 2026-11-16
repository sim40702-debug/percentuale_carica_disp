// pagina-impostazioni.js
// Fa funzionare la finestra Impostazioni.
// Ogni modifica viene salvata subito (non c'è un pulsante "Salva"), come su Apple.

// Gli interruttori semplici: id nell'HTML = nome dell'impostazione
const INTERRUTTORI = ["mostraPercentuale", "animazioni", "avvisoCompleta", "avvioWindows"];

async function carica() {
  const dati = await window.app.leggiImpostazioni();
  const impostazioni = dati.impostazioni;

  mostraDispositivi(dati.dispositivi);

  // Interruttori: acceso o spento in base alle impostazioni
  for (let i = 0; i < INTERRUTTORI.length; i++) {
    const chiave = INTERRUTTORI[i];
    document.getElementById(chiave).checked = impostazioni[chiave];
  }

  // Soglia: evidenziamo il bottone giusto
  segnaSoglia(impostazioni.soglia);
}

// ----- Dispositivi -----

function mostraDispositivi(dispositivi) {
  const contenitore = document.getElementById("dispositivi");
  contenitore.innerHTML = "";

  if (dispositivi.length === 0) {
    const testo = document.createElement("p");
    testo.className = "nessuno";
    testo.textContent = "Nessun dispositivo ancora. Collega cuffie, tastiera o controller e riapri questa finestra.";
    contenitore.appendChild(testo);
    return;
  }

  for (let i = 0; i < dispositivi.length; i++) {
    const d = dispositivi[i];

    if (i > 0) {
      const separatore = document.createElement("div");
      separatore.className = "separatore";
      contenitore.appendChild(separatore);
    }

    const riga = document.createElement("div");
    riga.className = "riga";

    // Icona
    const icona = document.createElement("div");
    icona.className = "icona-dispositivo";
    icona.innerHTML = creaIconaSVG(d.originale);
    riga.appendChild(icona);

    // Casella con il nome e, sotto, il nome originale
    const testi = document.createElement("div");
    testi.className = "testi";

    const campo = document.createElement("input");
    campo.type = "text";
    campo.className = "campo-nome";
    campo.value = d.nome;
    campo.placeholder = d.originale;   // se è vuota si vede il nome originale
    campo.maxLength = 30;

    // Quando finisci di scrivere (clic fuori o Invio), salviamo
    campo.addEventListener("change", function () {
      window.app.rinomina(d.originale, campo.value);
    });
    campo.addEventListener("keydown", function (evento) {
      if (evento.key === "Enter") {
        campo.blur();
      }
    });
    testi.appendChild(campo);

    const sotto = document.createElement("div");
    sotto.className = "sotto";
    if (d.batteria >= 0) {
      sotto.textContent = d.originale + " · " + d.batteria + "%";
    } else {
      sotto.textContent = d.originale + " · non collegato";
    }
    testi.appendChild(sotto);
    riga.appendChild(testi);

    // Interruttore "Sulla barra"
    const etichetta = document.createElement("span");
    etichetta.className = "etichetta";
    etichetta.textContent = "Sulla barra";
    riga.appendChild(etichetta);

    const interruttore = document.createElement("input");
    interruttore.type = "checkbox";
    interruttore.className = "interruttore";
    interruttore.checked = d.sullaBarra;
    interruttore.addEventListener("change", function () {
      window.app.sullaBarra(d.originale, interruttore.checked);
    });
    riga.appendChild(interruttore);

    contenitore.appendChild(riga);
  }
}

// ----- Interruttori -----

for (let i = 0; i < INTERRUTTORI.length; i++) {
  const chiave = INTERRUTTORI[i];
  const interruttore = document.getElementById(chiave);

  interruttore.addEventListener("change", function () {
    window.app.cambiaImpostazione(chiave, interruttore.checked);
  });
}

// ----- Soglia 10% / 20% / 30% -----

function segnaSoglia(valore) {
  const bottoni = document.querySelectorAll("#soglia button");
  for (let i = 0; i < bottoni.length; i++) {
    if (Number(bottoni[i].dataset.valore) === valore) {
      bottoni[i].classList.add("scelto");
    } else {
      bottoni[i].classList.remove("scelto");
    }
  }
}

const bottoniSoglia = document.querySelectorAll("#soglia button");
for (let i = 0; i < bottoniSoglia.length; i++) {
  bottoniSoglia[i].addEventListener("click", function () {
    const valore = Number(bottoniSoglia[i].dataset.valore);
    segnaSoglia(valore);
    window.app.cambiaImpostazione("soglia", valore);
  });
}

carica();
