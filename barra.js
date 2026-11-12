// barra.js
// Disegna le mini batterie sulla barra di Windows.

// Questa funzione la chiama main.js con la lista dei dispositivi collegati
function mostraBarra(lista) {
  const contenitore = document.getElementById("dispositivi");
  contenitore.innerHTML = "";

  // Nessun dispositivo collegato: mostriamo solo l'icona Bluetooth chiara
  if (lista.length === 0) {
    const vuoto = document.createElement("div");
    vuoto.className = "dispositivo vuoto";
    vuoto.innerHTML = creaIconaSVG("bluetooth");
    vuoto.title = "Nessun dispositivo collegato";
    contenitore.appendChild(vuoto);
    return;
  }

  for (let i = 0; i < lista.length; i++) {
    const nome = lista[i].nome;
    const percentuale = lista[i].batteria;

    const dispositivo = document.createElement("div");
    dispositivo.className = "dispositivo";
    dispositivo.title = nome + ": " + percentuale + "%";

    // Icona (cuffie, tastiera, mouse, controller)
    dispositivo.innerHTML = creaIconaSVG(nome);

    // Percentuale
    const testo = document.createElement("span");
    testo.className = "percentuale";
    testo.textContent = percentuale + "%";
    dispositivo.appendChild(testo);

    // Mini batteria che si riempie
    const batteria = document.createElement("div");
    batteria.className = "batteria";
    const carica = document.createElement("div");
    carica.className = "carica";
    carica.style.width = percentuale + "%";
    carica.style.background = coloreBatteria(percentuale);
    batteria.appendChild(carica);
    dispositivo.appendChild(batteria);

    contenitore.appendChild(dispositivo);
  }
}

// Clic sinistro: apre o chiude il widget
document.addEventListener("click", function () {
  window.app.clic();
});

// Clic destro: menu (Aggiorna, Sposta, Esci)
document.addEventListener("contextmenu", function (evento) {
  evento.preventDefault();
  window.app.menu();
});
