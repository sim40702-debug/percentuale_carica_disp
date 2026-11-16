# Batterie in Barra

Mini batterie stile Apple direttamente sulla barra di Windows, a sinistra delle icone vicino all'orologio, con un widget che si apre al clic.

## Come provarla

1. Apri la cartella in VS Code
2. Nel terminale scrivi `npm install`
3. Poi `npm start`

Su Mac l'app parte con dei **dati di prova**: le batterie vere si vedono solo su Windows.

## Come pubblicare una versione nuova

1. Cambia `"version"` in `package.json` (es. `0.1.1`)
2. Scrivi le novità in `docs/CHANGELOG.md`, in una sezione `## 0.1.1 — data (titolo)`
3. Fai commit e push, poi carica il tag: `git tag v0.1.1 && git push origin v0.1.1`

GitHub crea da solo l'exe e la release "Batterie in Barra 0.1.1" con le novità.
Si può anche far partire a mano da Actions → "Crea la release" → "Run workflow".

Per creare l'exe sul PC Windows senza GitHub: `npm run crea-exe` (finisce nella cartella `dist`).

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
