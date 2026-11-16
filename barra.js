// barra.js
// Disegna le mini batterie sulla barra di Windows, con la dissolvenza
// quando un dispositivo si collega o si scollega.

// Gli elementi già sulla barra: nome originale -> elemento HTML
let elementi = {};

// Questa funzione la chiama main.js con:
// - lista: i dispositivi da mostrare
// - opzioni: { mostraPercentuale, animazioni }
function mostraBarra(lista, opzioni) {
  const contenitore = document.getElementById("dispositivi");

  // Le opzioni diventano classi sul body (il CSS fa il resto)
  document.body.classList.toggle("senza-percentuale", !opzioni.mostraPercentuale);
  document.body.classList.toggle("senza-animazioni", !opzioni.animazioni);

  // Nessun dispositivo: mostriamo solo l'icona Bluetooth chiara
  if (lista.length === 0) {
    lista = [{ originale: "nessuno", nome: "Nessun dispositivo collegato", batteria: -1 }];
  }

  // 1. Togliamo (con dissolvenza) quelli che non ci sono più
  const nomiNuovi = [];
  for (let i = 0; i < lista.length; i++) {
    nomiNuovi.push(lista[i].originale);
  }

  for (const originale in elementi) {
    if (!nomiNuovi.includes(originale)) {
      togliElemento(elementi[originale]);
      delete elementi[originale];
    }
  }

  // 2. Aggiorniamo quelli che ci sono e aggiungiamo quelli nuovi
  for (let i = 0; i < lista.length; i++) {
    const dati = lista[i];
    let elemento = elementi[dati.originale];

    if (elemento === undefined) {
      elemento = document.createElement("div");
      elemento.className = "dispositivo entra";   // "entra" = dissolvenza in entrata
      elementi[dati.originale] = elemento;

      // Finita la dissolvenza togliamo la classe, così non riparte a ogni aggiornamento
      elemento.addEventListener("animationend", function () {
        elemento.classList.remove("entra");
      });
    }

    riempiElemento(elemento, dati);
    contenitore.appendChild(elemento);   // lo mette (o lo sposta) nell'ordine giusto
  }
}

// Scrive dentro l'elemento: icona, percentuale e mini batteria
function riempiElemento(elemento, dati) {
  // Caso speciale: nessun dispositivo
  if (dati.batteria < 0) {
    elemento.classList.add("vuoto");
    elemento.innerHTML = creaIconaSVG("bluetooth");
    elemento.title = dati.nome;
    return;
  }

  elemento.title = dati.nome + ": " + dati.batteria + "%";

  // Icona (la scegliamo dal nome originale, così funziona anche se lo rinomini)
  elemento.innerHTML = creaIconaSVG(dati.originale);

  // Percentuale
  const testo = document.createElement("span");
  testo.className = "percentuale";
  testo.textContent = dati.batteria + "%";
  elemento.appendChild(testo);

  // Mini batteria che si riempie
  const batteria = document.createElement("div");
  batteria.className = "batteria";
  const carica = document.createElement("div");
  carica.className = "carica";
  carica.style.width = dati.batteria + "%";
  carica.style.background = coloreBatteria(dati.batteria);
  batteria.appendChild(carica);
  elemento.appendChild(batteria);
}

// Dissolvenza in uscita, poi lo togliamo davvero
function togliElemento(elemento) {
  if (document.body.classList.contains("senza-animazioni")) {
    elemento.remove();
    return;
  }
  elemento.classList.add("esce");
  setTimeout(function () {
    elemento.remove();
  }, 350);
}

// Clic sinistro: apre o chiude il widget
document.addEventListener("click", function () {
  window.app.clic();
});

// Clic destro: menu (Aggiorna, Impostazioni, Sposta, Esci)
document.addEventListener("contextmenu", function (evento) {
  evento.preventDefault();
  window.app.menu();
});
