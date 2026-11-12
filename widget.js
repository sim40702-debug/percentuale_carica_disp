// widget.js
// Disegna il widget stile Apple e le mini batterie per la barra.

// ----- Colori -----

// Sceglie il colore in base alla percentuale
function coloreBatteria(percentuale) {
  if (percentuale <= 20) {
    return "#ff3b30"; // rosso
  }
  if (percentuale <= 40) {
    return "#ffcc00"; // giallo
  }
  return "#34c759";   // verde
}


// ----- Icone dei dispositivi (disegni SVG semplici) -----

const ICONE = {
  cuffie: '<path d="M4 15v-3a8 8 0 0 1 16 0v3"/><rect x="3" y="14" width="4" height="7" rx="1.5"/><rect x="17" y="14" width="4" height="7" rx="1.5"/>',
  tastiera: '<rect x="2" y="6" width="20" height="12" rx="2.5"/><path d="M6 10h.01M10 10h.01M14 10h.01M18 10h.01M7 14h10"/>',
  mouse: '<rect x="6" y="3" width="12" height="18" rx="6"/><path d="M12 7v4"/>',
  controller: '<path d="M6 8h12a4 4 0 0 1 4 4l-.8 4.5a2 2 0 0 1-3.4 1.1L15.5 15h-7l-2.3 2.6a2 2 0 0 1-3.4-1.1L2 12a4 4 0 0 1 4-4z"/><path d="M7.5 10.5v3M6 12h3M16 11h.01M18 13h.01"/>',
  bluetooth: '<path d="M7 7l10 10-5 5V2l5 5L7 17"/>'
};

// Indovina il tipo di dispositivo dal nome
function tipoDispositivo(nome) {
  const n = nome.toLowerCase();
  const parole = {
    cuffie: ["cuffie", "head", "buds", "airpods", "earbuds", "wh-", "wf-", "jbl", "bose", "beats", "auricolari"],
    tastiera: ["keyboard", "tastiera", "keys", "k380", "k780"],
    mouse: ["mouse", "mx master", "mx anywhere"],
    controller: ["controller", "gamepad", "xbox", "dualsense", "dualshock", "joy-con", "pro controller"]
  };

  for (const tipo in parole) {
    for (let i = 0; i < parole[tipo].length; i++) {
      if (n.includes(parole[tipo][i])) {
        return tipo;
      }
    }
  }
  return "bluetooth";
}

function creaIconaSVG(nome) {
  const tipo = tipoDispositivo(nome);
  return '<svg class="icona" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" ' +
         'stroke-linecap="round" stroke-linejoin="round">' + ICONE[tipo] + '</svg>';
}


// ----- Il widget -----

// Questa funzione la chiama main.js con la lista dei dispositivi
function mostraDati(lista) {
  const cerchi = document.getElementById("cerchi");
  const elenco = document.getElementById("lista");
  const vuoto = document.getElementById("vuoto");

  // Svuotiamo tutto prima di ridisegnare
  cerchi.innerHTML = "";
  elenco.innerHTML = "";

  if (lista.length === 0) {
    vuoto.style.display = "block";
  } else {
    vuoto.style.display = "none";
  }

  for (let i = 0; i < lista.length; i++) {
    const nome = lista[i].nome;
    const percentuale = lista[i].batteria;
    const colore = coloreBatteria(percentuale);

    // --- Cerchio con l'anello ---
    const raggio = 26;
    const circonferenza = 2 * Math.PI * raggio;
    const pieno = circonferenza * percentuale / 100;

    const cerchio = document.createElement("div");
    cerchio.className = "cerchio";
    cerchio.innerHTML =
      '<div class="anello">' +
        '<svg width="60" height="60" viewBox="0 0 60 60">' +
          '<circle cx="30" cy="30" r="' + raggio + '" fill="none" class="anello-sfondo" stroke-width="4.5"/>' +
          '<circle cx="30" cy="30" r="' + raggio + '" fill="none" stroke="' + colore + '" stroke-width="4.5" ' +
            'stroke-linecap="round" stroke-dasharray="' + pieno + ' ' + circonferenza + '" transform="rotate(-90 30 30)"/>' +
        '</svg>' +
        creaIconaSVG(nome) +
      '</div>' +
      '<div class="percentuale">' + percentuale + '%</div>';
    cerchio.title = nome;
    cerchi.appendChild(cerchio);

    // --- Riga della lista ---
    const riga = document.createElement("div");
    riga.className = "riga";

    const testoNome = document.createElement("div");
    testoNome.className = "nome";
    testoNome.textContent = nome;   // textContent: il nome viene scritto così com'è

    if (percentuale <= 20) {
      const stato = document.createElement("span");
      stato.className = "stato";
      stato.textContent = "Batteria scarica";
      testoNome.appendChild(stato);
    }

    const numero = document.createElement("div");
    numero.className = "numero";
    numero.textContent = percentuale + "%";

    const batteria = document.createElement("div");
    batteria.className = "mini-batteria";
    const carica = document.createElement("div");
    carica.className = "carica";
    carica.style.width = percentuale + "%";
    carica.style.background = colore;
    batteria.appendChild(carica);

    riga.appendChild(testoNome);
    riga.appendChild(numero);
    riga.appendChild(batteria);
    elenco.appendChild(riga);
  }
}


// ----- Mini batteria per la barra -----

// Rettangolo con gli angoli arrotondati
function rettangoloArrotondato(ctx, x, y, larghezza, altezza, raggio) {
  ctx.beginPath();
  ctx.roundRect(x, y, larghezza, altezza, raggio);
}

// Disegna una mini batteria quadrata (lato = 16, 24 o 32 pixel)
// e restituisce l'immagine come testo (dataURL), che main.js usa per l'icona.
// Se la percentuale è -1 disegna una batteria vuota con il "?"
function disegnaMiniBatteria(percentuale, lato) {
  const canvas = document.createElement("canvas");
  canvas.width = lato;
  canvas.height = lato;
  const ctx = canvas.getContext("2d");
  const L = lato;

  // Misure della batteria (in proporzione al lato)
  const x = L * 0.02;
  const y = L * 0.2;
  const larghezza = L * 0.86;
  const altezza = L * 0.6;
  const spessore = Math.max(1, L * 0.07);

  // Sfondo scuro dentro la batteria (così il numero si legge sempre)
  rettangoloArrotondato(ctx, x, y, larghezza, altezza, L * 0.15);
  ctx.fillStyle = "rgba(0, 0, 0, 0.6)";
  ctx.fill();

  // Riempimento colorato
  if (percentuale > 0) {
    const margine = spessore + L * 0.03;
    const larghezzaPiena = (larghezza - margine * 2) * percentuale / 100;
    rettangoloArrotondato(ctx, x + margine, y + margine, larghezzaPiena, altezza - margine * 2, L * 0.07);
    ctx.fillStyle = coloreBatteria(percentuale);
    ctx.fill();
  }

  // Bordo della batteria
  rettangoloArrotondato(ctx, x + spessore / 2, y + spessore / 2, larghezza - spessore, altezza - spessore, L * 0.13);
  ctx.strokeStyle = "#d1d1d6";
  ctx.lineWidth = spessore;
  ctx.stroke();

  // Punta della batteria
  rettangoloArrotondato(ctx, x + larghezza + L * 0.02, y + altezza * 0.3, L * 0.08, altezza * 0.4, L * 0.03);
  ctx.fillStyle = "#d1d1d6";
  ctx.fill();

  // Numero dentro la batteria
  let testo = String(percentuale);
  if (percentuale < 0) {
    testo = "?";
  }

  let grandezzaTesto = L * 0.46;
  if (testo.length === 3) {
    grandezzaTesto = L * 0.34;  // "100" è più lungo: lo facciamo più piccolo
  }

  ctx.font = "bold " + grandezzaTesto + "px 'Segoe UI', sans-serif";
  ctx.textAlign = "center";
  ctx.textBaseline = "middle";
  ctx.fillStyle = "#ffffff";
  ctx.fillText(testo, x + larghezza / 2, y + altezza / 2 + L * 0.02);

  return canvas.toDataURL("image/png");
}
