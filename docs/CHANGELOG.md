# CHANGELOG — Batterie in Barra

Formato delle versioni: MAGGIORE.MINORE.CORREZIONE (la versione viene dal tag, es. v0.3.6).
La sezione della versione diventa la descrizione della release su GitHub.

## 0.3.6 — 18/11/2026 (spegnimento del PC sistemato)
- **Il PC si spegne senza problemi**: prima, spegnendo il computer, a volte l'app mostrava una finestra di errore
  (il Bluetooth si stava già fermando) e Windows aspettava che la chiudessi prima di spegnersi.
  Adesso, quando Windows si spegne, l'app si ferma subito e in silenzio.
- Gli errori dei lavori in sottofondo vengono scritti nel registro invece di far chiudere l'app.

## 0.3.5 — 18/11/2026 (foto nella descrizione)
- **Foto nella pagina del progetto e nelle release**: la barra con le batterie, il widget, il menu e le impostazioni,
  così si vede subito com'è l'app.

## 0.3.3 — 18/11/2026 (menu spostato e fulmine più affidabile)
- **Il menu del tasto destro si apre più a sinistra**, fuori dalla zona delle notifiche di Windows:
  prima, se c'era una notifica, copriva il menu e non potevi cliccare "Impostazioni".
- **Fulmine più affidabile per cuffie, mouse e tastiere**: molte cuffie mandano la batteria a scatti di 10%,
  quindi il fulmine adesso resta acceso per un'ora dall'ultima salita (prima 20 minuti) e non si spegne tra uno scatto e l'altro.
- Quando l'app nota che un dispositivo sta caricando lo scrive nel registro (Informazioni per il supporto).

## 0.3.2 — 17/11/2026 (impostazioni a categorie e menu nuovo)
- **Impostazioni divise in 4 categorie**: Dispositivi, Aspetto, Avvisi e Generale. A sinistra scegli la categoria
  e a destra vedi solo quella, senza dover scorrere tutto. La finestra è più larga e meno alta.
- **Menu del tasto destro nuovo**: arrotondato come in Windows 11, con un'icona per ogni voce, chiaro o scuro
  come Windows. "Esci" è rosso e, quando c'è una versione nuova, la voce per aggiornare è verde in cima.

## 0.3.1 — 16/11/2026 (fulmine quando carica)
- **Fulmine nella batteria quando un dispositivo è in carica**: la batteria diventa verde
  con un fulmine giallo in mezzo, sulla barra e nel widget. Nel widget c'è scritto "In carica".
- Controller PS4/PS5, AirPods e batteria del PC dicono loro se sono in carica, quindi il fulmine compare subito.
  Per gli altri (cuffie, mouse, tastiere) Windows non lo sa: il fulmine compare quando la batteria sale di
  qualche punto e sparisce se smette di salire per 20 minuti.
- **L'icona dell'app adesso c'è davvero** (nella 0.3.0 erano arrivati solo i file dell'icona, non il codice che la usa):
  file dell'app, menu Start, installer, finestre e notifiche.

## 0.3.0 — 16/11/2026 (icona nuova)
- **L'app ha la sua icona**: tre mini batterie (verde, gialla, rossa) su sfondo blu.
  La vedi sul file dell'app, nel menu Start, nell'installer, nelle finestre e nelle notifiche.
- A 16 pixel (dove c'è poco spazio) l'icona è semplificata, così si capisce lo stesso.

## 0.2.9 — 16/11/2026 (controller PlayStation)
- **Controller PS4 (DualShock 4) e PS5 (DualSense)**: adesso compaiono anche loro, via Bluetooth e col cavo.
  Windows non conosce la loro batteria, quindi l'app la legge direttamente dai dati del controller (come fanno
  DS4Windows e Steam). Il PS4 va a scatti di circa 12% (il controller la manda così).
- Se il controller non compare: Impostazioni → Dati → Informazioni per il supporto → Copia, e manda il testo
  (c'è una parte "Controller PlayStation" con quello che l'app riceve).

## 0.2.8 — 16/11/2026 (barra di avanzamento per gli aggiornamenti)
- **Barra di avanzamento**: quando aggiorni l'app compare una finestrella con la barra, quanti MB sono stati
  scaricati (es. "24,5 MB di 61,2 MB · 40%") e il pulsante **Annulla**. Alla fine scrive "Installo e riavvio
  l'app…" e riparte da sola, con i tuoi dati.
- Se annulli (o chiudi la finestrella), non succede niente: l'app resta com'è e il file a metà viene cancellato.

## 0.2.7 — 16/11/2026 (installer e test automatici)
- **Installer**: oltre a `BatterieInBarra-0.2.7.exe` (da aprire così com'è) c'è anche
  `BatterieInBarra-Setup-0.2.7.exe`, che installa l'app come un programma normale: niente permessi da
  amministratore, menu Start, lista "App installate" di Windows e disinstallazione che toglie anche dati e avvio
  con Windows. L'aggiornamento automatico funziona in tutti e due i casi.
- **Test automatici**: a ogni modifica GitHub controlla da solo i calcoli dell'app (tempo stimato, durata delle
  cariche, salute della batteria, lettura delle AirPods, Non disturbare, colori, versioni). Se un test fallisce,
  la release non esce.
- "Elimina l'app dal PC" usa il disinstallatore, se l'app è stata installata con l'installer.

**Quale scaricare?** Se è la prima volta: `BatterieInBarra-Setup-0.2.7.exe`. Se hai già l'app, si aggiorna da sola.

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
