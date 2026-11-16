# CHANGELOG — Batterie in Barra

Formato delle versioni: MAGGIORE.MINORE.CORREZIONE (la versione viene dal tag, es. v0.2.6).
La sezione della versione diventa la descrizione della release su GitHub.

## 0.2.6 — 16/11/2026 (custodia AirPods, Non disturbare, scorciatoia, notifiche cliccabili)
- **Custodia delle AirPods**: nel widget, sotto le AirPods, compare anche la batteria della custodia (quando le
  AirPods la mandano, cioè con la custodia aperta e vicina).
- **Non disturbare**: niente avvisi di notte (scegli tu dalle 22, 23 o 00 fino alle 6, 7 o 8) e mentre giochi o
  guardi un video a schermo intero. Gli avvisi non si perdono: arrivano appena finisce il silenzio. Il promemoria
  serale arriva comunque all'ora che hai scelto (ma non durante un gioco).
- **Scorciatoia Ctrl + Alt + B**: apre e chiude il widget dalla tastiera (si può spegnere nelle Impostazioni).
- **Notifiche cliccabili**: cliccando un avviso (batteria scarica, carica completa...) si apre il widget su quel
  dispositivo.
- **Aggiornamento di riserva**: se l'aggiornamento automatico non riesce, l'app propone di aprire la pagina della
  release per scaricarla a mano. Da qualsiasi versione C# si passa sempre direttamente all'ultima, tenendo i dati.

## 0.2.5 — 16/11/2026 (mouse ritrovato, aggiornamento più sicuro, supporto)
- **Mouse e dispositivi che non comparivano**: su alcuni PC la ricerca nuova non trovava la batteria di qualche
  dispositivo (es. il mouse) che la prima versione invece vedeva. Adesso, solo se manca qualcuno, l'app usa come
  riserva il metodo della prima versione (al massimo ogni 2 minuti, così resta leggera).
- **Ricerca dei dispositivi collegati più sicura**: si cercano in due modi diversi (se uno non funziona, c'è l'altro).
- **Aggiornamento automatico più sicuro**: prima l'app cambiava il proprio file mentre era ancora accesa, e su
  alcuni PC si chiudeva senza ripartire. Adesso scarica la versione nuova, si chiude, e solo dopo un piccolo script
  la mette al suo posto (stesso nome, stessa cartella) e la riavvia.
- **Registro degli errori**: se qualcosa va storto l'app non si chiude più in silenzio, mostra un messaggio e lo
  scrive nel registro.
- **Informazioni per il supporto** (Impostazioni → Dati → Copia): copia cosa vede l'app (dispositivi, batterie,
  registro), da mandare se un dispositivo non compare.

**Se hai la 0.2.3 o la 0.2.4 e l'aggiornamento automatico si chiude senza ripartire**: scarica una volta
`BatterieInBarra-0.2.5.exe` qui sotto e aprilo (le impostazioni restano). Dalla 0.2.5 in poi l'aggiornamento
usa il metodo nuovo.

## 0.2.4 — 16/11/2026 (AirPods e Impostazioni più facili)
- **AirPods e cuffie Beats**: adesso compaiono anche loro. Windows non conosce la loro batteria, quindi l'app la
  legge dai messaggi Bluetooth che mandano le AirPods (come le app apposta per AirPods). La percentuale va a scatti
  di 10% ed è quella dell'auricolare più scarico. Se è appena partita, aspetta fino a un minuto.
- **Impostazioni più facili da trovare**: se l'app è già aperta e riapri `BatterieInBarra.exe`, si aprono le
  Impostazioni. Alla prima apertura un avviso spiega dove cliccare (clic = widget, tasto destro = menu).

## 0.2.3 — 16/11/2026 (statistiche, colori, promemoria e lingue)
- **Colori personalizzati**: batterie a colori oppure **bianche come su Apple** (rosse solo quando sono scariche).
  Scegli tu sotto quale % diventano gialle (30/40/50%) e rosse (10/15/20%).
- **Ordine dei dispositivi**: nelle Impostazioni, con le frecce ▲▼, decidi chi sta a sinistra e chi a destra.
- **Promemoria serale**: alle 21, 22 o 23 (scegli tu), se un dispositivo è sotto il 30% ti ricorda di caricarlo
  per domani.
- **Grafico di 7 giorni** oltre a quello di 24 ore: si sceglie in alto a destra nel widget.
- **Statistiche** nel widget: quanto dura in media una carica e quante volte l'hai caricato questo mese.
- **Salute della batteria**: se un dispositivo si scarica molto più in fretta di prima, ti avvisa che la batteria
  potrebbe essere vecchia.
- **Batteria del PC** nel widget, solo sui portatili (l'app lo capisce da sola; sui PC fissi l'opzione è spenta).
- **Inglese e tedesco**, oltre all'italiano, scelti in automatico dalla lingua di Windows.

## 0.2.0 — 16/11/2026 (riscritta in C#, molto più leggera)
- **Riscritta in C# (WPF)** al posto di Electron: usa molta meno memoria e parte più in fretta.
- **Niente più PowerShell**: le batterie e la posizione della barra si leggono direttamente da Windows, senza
  aprire programmi in sottofondo.
- **Un solo file .exe**: niente installazione, lo apri e parte. Mettilo in una cartella fissa (es. Documenti)
  se usi "Avvia con Windows".
- **Aggiornamento automatico**: ogni 6 ore cerca su GitHub una versione nuova. Se c'è, arriva un avviso e con
  tasto destro → "Aggiorna alla versione…" (o dalle Impostazioni) la scarica, sostituisce quella vecchia e riparte.
  **I tuoi dati restano.**
- **Sezione Dati** nelle Impostazioni: quanto spazio occupano i dati raccolti e il programma, apri la cartella,
  cancella la cronologia, ripristina le impostazioni oppure **elimina l'app dal PC** (dati, avvio con Windows e
  programma).
- Tutto il resto è come prima: mini batterie sulla barra, widget con tempo stimato e grafico, impostazioni,
  avvisi e animazioni.
- Le impostazioni ora si salvano in `%APPDATA%\BatterieInBarra`: nomi e dispositivi nascosti vanno rifatti una volta.

**Come si usa**: scarica `BatterieInBarra-0.2.0.exe` qui sotto e aprilo. Se Windows mostra "Windows ha protetto
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
