// comune.js
// Funzioni usate sia dalla barra sia dal widget: colori e icone.

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

// Icone dei dispositivi (disegni SVG semplici)
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
    controller: ["controller", "gamepad", "xbox", "dualsense", "dualshock", "joy-con"]
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

// Crea l'icona SVG del dispositivo
function creaIconaSVG(nome) {
  const tipo = tipoDispositivo(nome);
  return '<svg class="icona" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" ' +
         'stroke-linecap="round" stroke-linejoin="round">' + ICONE[tipo] + '</svg>';
}
