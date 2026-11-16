# Batterie in Barra

Mini batterie stile Apple direttamente sulla barra di Windows, a sinistra delle icone vicino all'orologio, con un widget che si apre al clic.

## Come provarla

1. Apri la cartella in VS Code
2. Nel terminale scrivi `npm install`
3. Poi `npm start`

Su Mac l'app parte con dei **dati di prova**: le batterie vere si vedono solo su Windows.

## Come creare il file .exe (sul PC Windows)

```
npm run crea-exe
```

Il programma di installazione finisce nella cartella `dist`.

## I file

- `main.js` - avvia l'app, mette le batterie sulla barra, apre widget e impostazioni
- `batteria.js` - chiede a Windows la batteria dei dispositivi collegati (con PowerShell)
- `posizione.js` - controlla dove si trova la barra di Windows e se c'è qualcosa a schermo intero
- `impostazioni.js` - legge e salva le impostazioni
- `cronologia.js` - si ricorda la batteria nel tempo (grafico e tempo stimato)
- `preload.js` - il ponte tra le pagine e main.js
- `barra.html` / `barra.css` / `barra.js` - le mini batterie sulla barra
- `widget.html` / `widget.css` / `widget.js` - il widget stile Apple con tempo stimato e grafico
- `impostazioni.html` / `impostazioni.css` / `pagina-impostazioni.js` - la finestra Impostazioni
- `comune.js` - colori e icone usati da tutte le pagine

## Comandi

- **Clic** sulle batterie: apre o chiude il widget
- **Tasto destro**: Aggiorna ora / Impostazioni / Sposta a sinistra / Sposta a destra / Esci

Le impostazioni e la cronologia vengono salvate nella cartella dei dati dell'app, non nel progetto.
