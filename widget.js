// widget.js
// Disegna il widget stile Apple (colori e icone sono in comune.js).

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
