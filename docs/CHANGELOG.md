# CHANGELOG — Batterie in Barra

Formato delle versioni: MAGGIORE.MINORE.CORREZIONE (la versione viene dal tag, es. v0.2.0).
La sezione della versione diventa la descrizione della release su GitHub.

## 0.1.3 — 16/11/2026 (riscritta in C#, molto più leggera)
- **Riscritta in C# (WPF)** al posto di Electron: usa molta meno memoria e parte più in fretta.
- **Niente più PowerShell**: le batterie e la posizione della barra si leggono direttamente da Windows, senza
  aprire programmi in sottofondo.
- **Un solo file .exe**: niente installazione, lo apri e parte. Mettilo in una cartella fissa (es. Documenti)
  se usi "Avvia con Windows".
- Tutto il resto è come prima: mini batterie sulla barra, widget con tempo stimato e grafico, impostazioni,
  avvisi e animazioni.
- Le impostazioni ora si salvano in `%APPDATA%\BatterieInBarra`: nomi e dispositivi nascosti vanno rifatti una volta.

**Come si usa**: scarica `BatterieInBarra-0.1.3.exe` qui sotto e aprilo. Se Windows mostra "Windows ha protetto
il PC", clicca "Ulteriori informazioni" e poi "Esegui comunque". Se avevi installato la 0.1.x, disinstallala da
Impostazioni → App.

## 0.1.2 — 16/11/2026 (prima versione)
- **Mini batterie sulla barra di Windows**, a sinistra delle icone vicino all'orologio, con icona, percentuale e
  colore (verde, giallo, rosso).
- **Solo i dispositivi Bluetooth collegati**: quelli spenti non compaiono più.
- **Widget** con un clic: cerchi, lista con il **tempo stimato** e **grafico delle ultime 24 ore**.
- **Impostazioni** (tasto destro → Impostazioni…): nomi personalizzati, dispositivi nascosti dalla barra, solo
  batteria senza percentuale, animazioni, avviso al 10/20/30%, avviso di carica completa, avvio con Windows.
- **Si nasconde da sola** con giochi e video a schermo intero.
