// widget.js
// Disegna il widget stile Apple (colori e icone sono in comune.js):
// cerchi, lista con il tempo stimato e grafico delle ultime 24 ore.

let ultimaLista = [];          // l'ultima lista ricevuta da main.js
let dispositivoGrafico = "";   // il dispositivo scelto per il grafico (nome originale)

// Questa funzione la chiama main.js con la lista dei dispositivi
function mostraDati(lista) {
  ultimaLista = lista;

  const cerchi = document.getElementById("cerchi");
  const elenco = document.getElementById("lista");
  const vuoto = document.getElementById("vuoto");
  const parteGrafico = document.getElementById("parte-grafico");

  // Svuotiamo tutto prima di ridisegnare
  cerchi.innerHTML = "";
  elenco.innerHTML = "";

  if (lista.length === 0) {
    vuoto.style.display = "block";
    parteGrafico.style.display = "none";
    return;
  }
  vuoto.style.display = "none";
  parteGrafico.style.display = "block";

  for (let i = 0; i < lista.length; i++) {
    cerchi.appendChild(creaCerchio(lista[i]));
    elenco.appendChild(creaRiga(lista[i]));
  }

  mostraGrafico();
}

// ----- Cerchio con l'anello -----

function creaCerchio(dati) {
  const raggio = 26;
  const circonferenza = 2 * Math.PI * raggio;
  const pieno = circonferenza * dati.batteria / 100;

  const cerchio = document.createElement("div");
  cerchio.className = "cerchio";
  cerchio.title = dati.nome;
  cerchio.innerHTML =
    '<div class="anello">' +
      '<svg width="60" height="60" viewBox="0 0 60 60">' +
        '<circle cx="30" cy="30" r="' + raggio + '" fill="none" class="anello-sfondo" stroke-width="4.5"/>' +
        '<circle cx="30" cy="30" r="' + raggio + '" fill="none" stroke="' + coloreBatteria(dati.batteria) + '" stroke-width="4.5" ' +
          'stroke-linecap="round" stroke-dasharray="' + pieno + ' ' + circonferenza + '" transform="rotate(-90 30 30)"/>' +
      '</svg>' +
      creaIconaSVG(dati.originale) +
    '</div>' +
    '<div class="percentuale">' + dati.batteria + '%</div>';
  return cerchio;
}

// ----- Riga della lista -----

function creaRiga(dati) {
  const riga = document.createElement("div");
  riga.className = "riga";

  // Nome e, sotto, il tempo stimato
  const testi = document.createElement("div");
  testi.className = "testi";

  const nome = document.createElement("div");
  nome.className = "nome";
  nome.textContent = dati.nome;   // textContent: il nome viene scritto così com'è
  testi.appendChild(nome);

  const tempo = document.createElement("div");
  tempo.className = "tempo";
  tempo.textContent = dati.tempo;
  if (dati.batteria <= 20) {
    tempo.classList.add("basso");
  }
  testi.appendChild(tempo);

  const numero = document.createElement("div");
  numero.className = "numero";
  numero.textContent = dati.batteria + "%";

  const batteria = document.createElement("div");
  batteria.className = "mini-batteria";
  const carica = document.createElement("div");
  carica.className = "carica";
  carica.style.width = dati.batteria + "%";
  carica.style.background = coloreBatteria(dati.batteria);
  batteria.appendChild(carica);

  riga.appendChild(testi);
  riga.appendChild(numero);
  riga.appendChild(batteria);
  return riga;
}

// ----- Grafico delle ultime 24 ore -----

function mostraGrafico() {
  // Se il dispositivo scelto non c'è più, prendiamo il primo
  let scelto = null;
  for (let i = 0; i < ultimaLista.length; i++) {
    if (ultimaLista[i].originale === dispositivoGrafico) {
      scelto = ultimaLista[i];
    }
  }
  if (scelto === null) {
    scelto = ultimaLista[0];
    dispositivoGrafico = scelto.originale;
  }

  // Bottoni per scegliere il dispositivo
  const bottoni = document.getElementById("scelta-grafico");
  bottoni.innerHTML = "";
  for (let i = 0; i < ultimaLista.length; i++) {
    const bottone = document.createElement("button");
    bottone.textContent = ultimaLista[i].nome;
    if (ultimaLista[i].originale === dispositivoGrafico) {
      bottone.className = "scelto";
    }
    const originale = ultimaLista[i].originale;
    bottone.addEventListener("click", function () {
      dispositivoGrafico = originale;
      mostraGrafico();
    });
    bottoni.appendChild(bottone);
  }

  document.getElementById("grafico").innerHTML = disegnaGrafico(scelto.storia, coloreBatteria(scelto.batteria));
}

// Crea il grafico in SVG (larghezza 280, altezza 80)
function disegnaGrafico(punti, colore) {
  const L = 280;
  const A = 80;
  const VENTIQUATTRO_ORE = 24 * 60 * 60 * 1000;
  const VENTI_MINUTI = 20 * 60 * 1000;
  const adesso = Date.now();
  const inizio = adesso - VENTIQUATTRO_ORE;

  if (punti.length < 2) {
    return '<p class="pochi-dati">Ancora pochi dati: il grafico si riempie con il tempo.</p>';
  }

  // Trasforma ora e percentuale in coordinate x e y
  function x(t) {
    return ((t - inizio) / VENTIQUATTRO_ORE * L).toFixed(1);
  }
  function y(p) {
    return (A - p / 100 * (A - 4)).toFixed(1);
  }

  // Dividiamo in pezzi: se per più di 20 minuti non ci sono dati, il dispositivo era scollegato
  const pezzi = [];
  let pezzo = [punti[0]];
  for (let i = 1; i < punti.length; i++) {
    if (punti[i].t - punti[i - 1].t > VENTI_MINUTI) {
      pezzi.push(pezzo);
      pezzo = [];
    }
    pezzo.push(punti[i]);
  }
  pezzi.push(pezzo);

  let disegno =
    '<svg width="100%" viewBox="0 0 ' + L + ' ' + (A + 18) + '">' +
    '<defs><linearGradient id="sfumatura" x1="0" y1="0" x2="0" y2="1">' +
      '<stop offset="0" stop-color="' + colore + '" stop-opacity="0.35"/>' +
      '<stop offset="1" stop-color="' + colore + '" stop-opacity="0"/>' +
    '</linearGradient></defs>' +
    '<line x1="0" y1="' + y(50) + '" x2="' + L + '" y2="' + y(50) + '" class="griglia" stroke-dasharray="3 4"/>' +
    '<line x1="0" y1="' + A + '" x2="' + L + '" y2="' + A + '" class="griglia"/>';

  for (let i = 0; i < pezzi.length; i++) {
    const p = pezzi[i];
    let linea = "M" + x(p[0].t) + "," + y(p[0].p);
    for (let j = 1; j < p.length; j++) {
      linea = linea + " L" + x(p[j].t) + "," + y(p[j].p);
    }
    // Se il pezzo arriva fino ad adesso, lo allunghiamo fino al bordo destro
    const ultimo = p[p.length - 1];
    let fine = x(ultimo.t);
    if (adesso - ultimo.t < VENTI_MINUTI) {
      linea = linea + " L" + L + "," + y(ultimo.p);
      fine = L;
    }
    // L'area colorata sotto la linea
    const area = linea + " L" + fine + "," + A + " L" + x(p[0].t) + "," + A + " Z";

    disegno = disegno +
      '<path d="' + area + '" fill="url(#sfumatura)"/>' +
      '<path d="' + linea + '" fill="none" stroke="' + colore + '" stroke-width="2" stroke-linejoin="round"/>';
  }

  disegno = disegno +
    '<text x="0" y="' + (A + 14) + '" class="etichetta">24 ore fa</text>' +
    '<text x="' + (L / 2) + '" y="' + (A + 14) + '" class="etichetta" text-anchor="middle">12 ore fa</text>' +
    '<text x="' + L + '" y="' + (A + 14) + '" class="etichetta" text-anchor="end">adesso</text>' +
    '</svg>';

  return disegno;
}
