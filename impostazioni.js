// impostazioni.js
// Legge e salva le impostazioni dell'app in un file JSON.
// Il file sta nella cartella dei dati dell'app (non nel progetto).

const { app } = require("electron");
const fs = require("fs");
const path = require("path");

const fileImpostazioni = path.join(app.getPath("userData"), "impostazioni.json");

// Valori di partenza, usati finché non cambi qualcosa
const PREDEFINITE = {
  nomi: {},                 // nome originale -> nome scelto da te (es. "WH-1000XM5" -> "Cuffie")
  nascosti: [],             // dispositivi da non mostrare sulla barra
  visti: [],                // tutti i dispositivi visti almeno una volta
  mostraPercentuale: true,  // percentuale sulla barra
  animazioni: true,         // dissolvenza quando si collega o si scollega
  soglia: 20,               // avviso batteria scarica (10, 20 o 30)
  avvisoCompleta: true,     // avviso quando torna al 100%
  avvioWindows: true,       // parte da sola all'accensione
  spostamento: 0            // pixel spostati a mano con il menu
};

function leggiImpostazioni() {
  let salvate = {};
  try {
    salvate = JSON.parse(fs.readFileSync(fileImpostazioni, "utf8"));
  } catch (e) {
    // il file non c'è ancora: usiamo solo i valori di partenza
  }
  // Uniamo: quello che è salvato vince sui valori di partenza
  return Object.assign({}, PREDEFINITE, salvate);
}

function salvaImpostazioni(impostazioni) {
  fs.writeFileSync(fileImpostazioni, JSON.stringify(impostazioni, null, 2));
}

module.exports = { leggiImpostazioni, salvaImpostazioni };
