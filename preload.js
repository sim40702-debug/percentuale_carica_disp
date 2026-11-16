// preload.js
// Il "ponte" tra le pagine (barra e impostazioni) e main.js.

const { contextBridge, ipcRenderer } = require("electron");

contextBridge.exposeInMainWorld("app", {
  // ----- Barra -----

  // clic sinistro: apre o chiude il widget
  clic: function () {
    ipcRenderer.send("clic-barra");
  },
  // clic destro: apre il menu
  menu: function () {
    ipcRenderer.send("menu-barra");
  },

  // ----- Impostazioni -----

  // chiede a main.js le impostazioni e i dispositivi
  leggiImpostazioni: function () {
    return ipcRenderer.invoke("leggi-impostazioni");
  },
  // cambia un'impostazione (es. "soglia", 30)
  cambiaImpostazione: function (chiave, valore) {
    ipcRenderer.send("cambia-impostazione", chiave, valore);
  },
  // cambia il nome di un dispositivo
  rinomina: function (originale, nome) {
    ipcRenderer.send("rinomina", originale, nome);
  },
  // mostra o nasconde un dispositivo sulla barra
  sullaBarra: function (originale, mostra) {
    ipcRenderer.send("sulla-barra", originale, mostra);
  }
});
