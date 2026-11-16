// cronologia.js
// Si ricorda la batteria nel tempo, per:
// - il grafico delle ultime 24 ore
// - il tempo stimato ("circa 6 ore rimaste")

const { app } = require("electron");
const fs = require("fs");
const path = require("path");

const fileCronologia = path.join(app.getPath("userData"), "cronologia.json");

const UN_MINUTO = 60 * 1000;
const UN_ORA = 60 * UN_MINUTO;
const SETTE_GIORNI = 7 * 24 * UN_ORA;
const OGNI_QUANTO_SALVARE = 10 * UN_MINUTO;  // salviamo un punto ogni 10 minuti (o se cambia la %)
const PAUSA_MASSIMA = 20 * UN_MINUTO;        // più di 20 minuti senza dati = dispositivo scollegato

// Struttura: { "WH-1000XM5": [ { t: ora in millisecondi, p: percentuale }, ... ] }
let cronologia = {};
try {
  cronologia = JSON.parse(fs.readFileSync(fileCronologia, "utf8"));
} catch (e) {
  cronologia = {};
}

// Aggiunge una lettura della batteria
function aggiungiLettura(nome, percentuale) {
  const adesso = Date.now();

  if (cronologia[nome] === undefined) {
    cronologia[nome] = [];
  }
  const punti = cronologia[nome];
  const ultimo = punti[punti.length - 1];

  // Salviamo solo se la % è cambiata o se sono passati 10 minuti
  if (ultimo !== undefined && ultimo.p === percentuale && adesso - ultimo.t < OGNI_QUANTO_SALVARE) {
    return;
  }

  punti.push({ t: adesso, p: percentuale });

  // Togliamo i punti più vecchi di 7 giorni
  while (punti.length > 0 && adesso - punti[0].t > SETTE_GIORNI) {
    punti.shift();
  }

  fs.writeFileSync(fileCronologia, JSON.stringify(cronologia));
}

// I punti delle ultime 24 ore (per il grafico)
function ultime24Ore(nome) {
  const punti = cronologia[nome] || [];
  const inizio = Date.now() - 24 * UN_ORA;
  const risultato = [];

  for (let i = 0; i < punti.length; i++) {
    if (punti[i].t >= inizio) {
      risultato.push(punti[i]);
    }
  }
  return risultato;
}

// Calcola quanto tempo rimane, guardando quanto scende la batteria dall'ultima carica
function tempoStimato(nome) {
  const punti = cronologia[nome] || [];
  if (punti.length < 2) {
    return "Calcolo in corso…";
  }

  // 1. Troviamo l'ultima volta che la batteria è salita (= è stata caricata)
  let inizio = 0;
  for (let i = 1; i < punti.length; i++) {
    if (punti[i].p > punti[i - 1].p) {
      inizio = i;
    }
  }

  // 2. Quanto tempo è stato collegato da allora (senza contare le pause)
  let tempoCollegato = 0;
  for (let i = inizio + 1; i < punti.length; i++) {
    const pausa = punti[i].t - punti[i - 1].t;
    if (pausa < PAUSA_MASSIMA) {
      tempoCollegato = tempoCollegato + pausa;
    }
  }

  // 3. Quanto è scesa la batteria
  const ultimo = punti[punti.length - 1];
  const scesa = punti[inizio].p - ultimo.p;

  // Servono almeno 3 punti di differenza e mezz'ora di dati
  if (scesa < 3 || tempoCollegato < 30 * UN_MINUTO) {
    return "Calcolo in corso…";
  }

  // 4. Tempo per ogni punto % e tempo rimasto
  const tempoPerPunto = tempoCollegato / scesa;
  const oreRimaste = (ultimo.p * tempoPerPunto) / UN_ORA;

  if (oreRimaste < 1) {
    return "meno di 1 ora rimasta";
  }
  if (oreRimaste < 1.5) {
    return "circa 1 ora rimasta";
  }
  if (oreRimaste < 48) {
    return "circa " + Math.round(oreRimaste) + " ore rimaste";
  }
  return "circa " + Math.round(oreRimaste / 24) + " giorni rimasti";
}

module.exports = { aggiungiLettura, ultime24Ore, tempoStimato };
