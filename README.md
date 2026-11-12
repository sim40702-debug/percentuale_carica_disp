# Batterie in Barra

Mini batterie stile Apple vicino all'orologio di Windows, con un widget che si apre al clic.

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

- `main.js` - avvia l'app, mette le mini batterie nella barra, apre il widget
- `batteria.js` - chiede a Windows le batterie (con PowerShell)
- `widget.html` / `widget.css` / `widget.js` - il widget stile Apple e il disegno delle mini batterie

## Comandi

- **Clic** su una mini batteria: apre o chiude il widget
- **Tasto destro**: Aggiorna ora / Esci
