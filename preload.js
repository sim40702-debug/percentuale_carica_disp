// preload.js
// Il "ponte" tra la barra (la pagina) e main.js:
// quando clicchi sulla barra, avvisa main.js.

const { contextBridge, ipcRenderer } = require("electron");

contextBridge.exposeInMainWorld("app", {
  // clic sinistro: apre o chiude il widget
  clic: function () {
    ipcRenderer.send("clic-barra");
  },
  // clic destro: apre il menu
  menu: function () {
    ipcRenderer.send("menu-barra");
  }
});
