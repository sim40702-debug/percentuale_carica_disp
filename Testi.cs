// Testi.cs
// Tutte le scritte dell'app in italiano, inglese e tedesco.
// La lingua viene scelta in automatico da quella di Windows (se non è IT/DE si usa l'inglese).
//
// Nel codice:  Testi.T("chiave")              -> la scritta nella lingua giusta
//              Testi.T("chiave", valore, ...)  -> con i valori al posto di {0}, {1}...
// Nel XAML:    Text="{local:Testo chiave}"

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Markup;

namespace BatterieInBarra
{
    public static class Testi
    {
        // 0 = italiano, 1 = inglese, 2 = tedesco
        static int lingua = SceltaLingua();

        // Cambia la lingua: "it", "en", "de" oppure "auto" (quella di Windows).
        // Va chiamata prima di aprire le finestre (le scritte vengono lette quando si aprono)
        public static void ImpostaLingua(string codice)
        {
            if (codice == "it")
            {
                lingua = 0;
            }
            else if (codice == "en")
            {
                lingua = 1;
            }
            else if (codice == "de")
            {
                lingua = 2;
            }
            else
            {
                lingua = SceltaLingua();
            }
        }

        static int SceltaLingua()
        {
            string codice = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (codice == "it") return 0;
            if (codice == "de") return 2;
            return 1;
        }

        public static string T(string chiave, params object[] valori)
        {
            string[] traduzioni;
            if (!TUTTI.TryGetValue(chiave, out traduzioni))
            {
                return chiave;   // chiave sbagliata: si vede subito quale manca
            }
            string testo = traduzioni[lingua];
            if (valori.Length > 0)
            {
                testo = string.Format(testo, valori);
            }
            return testo;
        }

        // Trasforma i byte in un testo leggibile (es. "512 byte", "12,5 KB", "61,2 MB")
        public static string Grandezza(long byteTotali)
        {
            if (byteTotali < 1024)
            {
                return byteTotali + " byte";
            }
            if (byteTotali < 1024 * 1024)
            {
                return Math.Round(byteTotali / 1024.0, 1) + " KB";
            }
            return Math.Round(byteTotali / (1024.0 * 1024.0), 1) + " MB";
        }

        // chiave -> { italiano, inglese, tedesco }
        static readonly Dictionary<string, string[]> TUTTI = new Dictionary<string, string[]>
        {
            // ----- Barra e menu -----
            { "nessun_dispositivo", new[] { "Nessun dispositivo collegato", "No device connected", "Kein Gerät verbunden" } },
            { "menu_aggiorna_versione", new[] { "Aggiorna alla versione {0}", "Update to version {0}", "Auf Version {0} aktualisieren" } },
            { "menu_aggiorna_batterie", new[] { "Aggiorna batterie", "Refresh batteries", "Akkus aktualisieren" } },
            { "menu_impostazioni", new[] { "Impostazioni…", "Settings…", "Einstellungen…" } },
            { "menu_sinistra", new[] { "Sposta a sinistra", "Move left", "Nach links" } },
            { "menu_destra", new[] { "Sposta a destra", "Move right", "Nach rechts" } },
            { "menu_esci", new[] { "Esci", "Quit", "Beenden" } },

            // ----- Widget -----
            { "batterie", new[] { "Batterie", "Batteries", "Akkus" } },
            { "nessun_collegato", new[] { "Nessun dispositivo collegato.", "No device connected.", "Kein Gerät verbunden." } },
            { "cronologia", new[] { "Cronologia", "History", "Verlauf" } },
            { "24_ore", new[] { "24 ore", "24 hours", "24 Stunden" } },
            { "7_giorni", new[] { "7 giorni", "7 days", "7 Tage" } },
            { "pochi_dati", new[] { "Ancora pochi dati: il grafico si riempie con il tempo.", "Not much data yet: the chart fills up over time.", "Noch wenige Daten: das Diagramm füllt sich mit der Zeit." } },
            { "24_ore_fa", new[] { "24 ore fa", "24 h ago", "vor 24 Std." } },
            { "12_ore_fa", new[] { "12 ore fa", "12 h ago", "vor 12 Std." } },
            { "7_giorni_fa", new[] { "7 giorni fa", "7 days ago", "vor 7 Tagen" } },
            { "4_giorni_fa", new[] { "4 giorni fa", "4 days ago", "vor 4 Tagen" } },
            { "adesso", new[] { "adesso", "now", "jetzt" } },
            { "dura_carica", new[] { "Una carica dura circa {0}", "A full charge lasts about {0}", "Eine Ladung hält etwa {0}" } },
            { "durata_da_calcolare", new[] { "Durata di una carica: ancora da calcolare", "Charge duration: not calculated yet", "Ladedauer: noch nicht berechnet" } },
            { "cariche_mese", new[] { "Caricato {0} volte questo mese", "Charged {0} times this month", "Diesen Monat {0}-mal geladen" } },
            { "salute", new[] { "Si scarica più in fretta del solito: la batteria potrebbe essere vecchia", "Draining faster than usual: the battery might be getting old", "Entlädt sich schneller als sonst: der Akku könnte alt sein" } },
            { "in_carica", new[] { "In carica", "Charging", "Wird geladen" } },
            { "custodia", new[] { "custodia {0}%", "case {0}%", "Etui {0}%" } },
            { "questo_pc", new[] { "Questo PC", "This PC", "Dieser PC" } },
            { "ore", new[] { "{0} ore", "{0} hours", "{0} Stunden" } },
            { "giorni", new[] { "{0} giorni", "{0} days", "{0} Tage" } },

            // ----- Tempo stimato -----
            { "calcolo", new[] { "Calcolo in corso…", "Calculating…", "Wird berechnet…" } },
            { "meno_1_ora", new[] { "meno di 1 ora rimasta", "less than 1 hour left", "weniger als 1 Stunde übrig" } },
            { "circa_1_ora", new[] { "circa 1 ora rimasta", "about 1 hour left", "etwa 1 Stunde übrig" } },
            { "circa_ore", new[] { "circa {0} ore rimaste", "about {0} hours left", "etwa {0} Stunden übrig" } },
            { "circa_giorni", new[] { "circa {0} giorni rimasti", "about {0} days left", "etwa {0} Tage übrig" } },

            // ----- Notifiche -----
            { "avviso_scarica", new[] { "Batteria scarica", "Battery low", "Akku schwach" } },
            { "avviso_scarica_testo", new[] { "{0} è al {1}%. Mettilo in carica!", "{0} is at {1}%. Time to charge it!", "{0} ist bei {1}%. Bitte aufladen!" } },
            { "avviso_custodia", new[] { "Custodia scarica", "Case battery low", "Ladecase schwach" } },
            { "avviso_custodia_testo", new[] { "La custodia di {0} è al {1}%. Mettila in carica!", "The {0} case is at {1}%. Time to charge it!", "Das Case von {0} ist bei {1}%. Bitte aufladen!" } },
            { "avviso_completa", new[] { "Carica completa", "Fully charged", "Vollständig geladen" } },
            { "avviso_completa_testo", new[] { "{0} è carico al 100%. Puoi staccarlo!", "{0} is at 100%. You can unplug it!", "{0} ist bei 100%. Du kannst es abstecken!" } },
            { "avviso_promemoria", new[] { "Promemoria", "Reminder", "Erinnerung" } },
            { "avviso_promemoria_testo", new[] { "Metti in carica {0} per domani: è al {1}%.", "Charge {0} for tomorrow: it's at {1}%.", "Lade {0} für morgen: es ist bei {1}%." } },
            { "avviso_salute", new[] { "Batteria da controllare", "Check the battery", "Akku prüfen" } },
            { "avviso_salute_testo", new[] { "{0} si scarica molto più in fretta del solito: la batteria potrebbe essere vecchia.", "{0} is draining much faster than usual: the battery might be getting old.", "{0} entlädt sich viel schneller als sonst: der Akku könnte alt sein." } },
            { "avviso_benvenuto", new[] { "Batterie in Barra è attiva", "Batterie in Barra is running", "Batterie in Barra läuft" } },
            { "avviso_benvenuto_testo", new[] { "Clic sulle batterie vicino all'orologio per il widget, tasto destro per le impostazioni.", "Click the batteries next to the clock for the widget, right-click for settings.", "Klicke auf die Akkus neben der Uhr für das Widget, Rechtsklick für die Einstellungen." } },
            { "avviso_aggiornata", new[] { "Aggiornata!", "Updated!", "Aktualisiert!" } },
            { "avviso_aggiornata_testo", new[] { "Batterie in Barra adesso è alla versione {0}.", "Batterie in Barra is now on version {0}.", "Batterie in Barra ist jetzt auf Version {0}." } },
            { "avviso_aggiornamento", new[] { "Aggiornamento disponibile", "Update available", "Update verfügbar" } },
            { "avviso_aggiornamento_testo", new[] { "La versione {0} è pronta. Tasto destro sulle batterie → Aggiorna.", "Version {0} is ready. Right-click the batteries → Update.", "Version {0} ist bereit. Rechtsklick auf die Akkus → Aktualisieren." } },

            // ----- Aggiornamenti -----
            { "stato_mai", new[] { "Non ancora controllato", "Not checked yet", "Noch nicht geprüft" } },
            { "stato_ultima", new[] { "Hai l'ultima versione", "You have the latest version", "Du hast die neueste Version" } },
            { "stato_disponibile", new[] { "È disponibile la versione {0}", "Version {0} is available", "Version {0} ist verfügbar" } },
            { "stato_errore", new[] { "Non riesco a controllare (sei offline o il repository non è pubblico)", "Can't check (you're offline or the repository isn't public)", "Prüfung nicht möglich (offline oder Repository nicht öffentlich)" } },
            { "errore_generico", new[] { "Batterie in Barra ha avuto un problema (l'app continua a funzionare).\nSe succede di nuovo: Impostazioni → Dati → Informazioni per il supporto → Copia, e manda il testo.", "Batterie in Barra ran into a problem (the app keeps running).\nIf it happens again: Settings → Data → Support information → Copy, and send the text.", "Batterie in Barra hatte ein Problem (die App läuft weiter).\nWenn es wieder passiert: Einstellungen → Daten → Support-Informationen → Kopieren, und den Text senden." } },
            { "apri_pagina_release", new[] { "Vuoi aprire la pagina per scaricarla a mano? (le impostazioni restano)", "Open the page to download it manually? (your settings are kept)", "Seite öffnen, um sie manuell herunterzuladen? (Einstellungen bleiben erhalten)" } },
            { "agg_titolo", new[] { "Aggiornamento alla versione {0}", "Updating to version {0}", "Update auf Version {0}" } },
            { "agg_scarico", new[] { "Scarico la versione nuova…", "Downloading the new version…", "Neue Version wird geladen…" } },
            { "agg_avanzamento", new[] { "{0} di {1} · {2}%", "{0} of {1} · {2}%", "{0} von {1} · {2}%" } },
            { "agg_avanzamento_senza_totale", new[] { "{0} scaricati", "{0} downloaded", "{0} geladen" } },
            { "agg_installo", new[] { "Installo e riavvio l'app… i tuoi dati restano.", "Installing and restarting… your data is kept.", "Installiere und starte neu… deine Daten bleiben erhalten." } },
            { "annulla", new[] { "Annulla", "Cancel", "Abbrechen" } },
            { "errore_aggiornamento", new[] { "Non sono riuscito ad aggiornare l'app.", "The app could not be updated.", "Die App konnte nicht aktualisiert werden." } },

            // ----- Impostazioni -----
            { "impostazioni_breve", new[] { "Impostazioni", "Settings", "Einstellungen" } },
            { "categoria_dispositivi", new[] { "Dispositivi", "Devices", "Geräte" } },
            { "categoria_aspetto", new[] { "Aspetto", "Appearance", "Darstellung" } },
            { "categoria_avvisi", new[] { "Avvisi", "Notifications", "Benachrichtigungen" } },
            { "categoria_generale", new[] { "Generale", "General", "Allgemein" } },
            { "sezione_icone", new[] { "Icone", "Icons", "Symbole" } },
            { "icone_spiegazione", new[] { "Scegli il disegno per ogni tipo di dispositivo, oppure carica il tuo",
                "Choose the design for each type of device, or upload your own",
                "Wähle das Symbol für jeden Gerätetyp oder lade dein eigenes hoch" } },
            { "icone_nota", new[] { "La tua immagine: PNG quadrato, meglio bianco su sfondo trasparente. Per tornare a un disegno, cliccalo.",
                "Your image: a square PNG, ideally white on a transparent background. To go back to a design, click it.",
                "Dein Bild: ein quadratisches PNG, am besten weiß auf transparentem Hintergrund. Um zu einem Symbol zurückzukehren, klicke es an." } },
            { "tipo_cuffie", new[] { "Cuffie", "Headphones", "Kopfhörer" } },
            { "tipo_controller", new[] { "Controller", "Controller", "Controller" } },
            { "tipo_mouse", new[] { "Mouse", "Mouse", "Maus" } },
            { "tipo_tastiera", new[] { "Tastiera", "Keyboard", "Tastatur" } },
            { "carica_icona", new[] { "Carica…", "Upload…", "Laden…" } },
            { "scegli_immagine", new[] { "Scegli un'immagine PNG", "Choose a PNG image", "PNG-Bild auswählen" } },
            { "immagine_grande", new[] { "L'immagine è troppo grande (massimo 2 MB).", "The image is too big (2 MB max).", "Das Bild ist zu groß (maximal 2 MB)." } },
            { "immagine_non_valida", new[] { "Non riesco ad aprire questa immagine. Prova con un altro file PNG.",
                "This image can't be opened. Try another PNG file.",
                "Dieses Bild lässt sich nicht öffnen. Versuche eine andere PNG-Datei." } },
            { "leggi_logitech", new[] { "Dispositivi Logitech", "Logitech devices", "Logitech-Geräte" } },
            { "leggi_logitech_sotto", new[] { "Mouse e tastiere con la chiavetta Unifying, Bolt o Lightspeed (anche il fulmine)",
                "Mice and keyboards with the Unifying, Bolt or Lightspeed receiver (with the charging bolt)",
                "Mäuse und Tastaturen mit Unifying-, Bolt- oder Lightspeed-Empfänger (mit Lade-Blitz)" } },
            { "leggi_ble", new[] { "Servizio batteria Bluetooth", "Bluetooth battery service", "Bluetooth-Akkudienst" } },
            { "leggi_ble_sotto", new[] { "Legge la batteria dei dispositivi Low Energy che Windows non mostra",
                "Reads the battery of Low Energy devices that Windows doesn't show",
                "Liest den Akku von Low-Energy-Geräten, die Windows nicht anzeigt" } },
            { "leggi_airpods", new[] { "AirPods e Beats", "AirPods and Beats", "AirPods und Beats" } },
            { "leggi_airpods_sotto", new[] { "Legge la batteria dai messaggi Bluetooth di Apple",
                "Reads the battery from Apple's Bluetooth messages",
                "Liest den Akku aus den Bluetooth-Nachrichten von Apple" } },
            { "avviso_custodia_riga", new[] { "Custodia AirPods scarica", "AirPods case low", "AirPods-Case schwach" } },
            { "avviso_custodia_sotto", new[] { "Quando la custodia scende sotto la soglia", "When the case drops below the threshold", "Wenn das Case unter die Schwelle fällt" } },
            { "sezione_per_dispositivo", new[] { "Per ogni dispositivo", "For each device", "Für jedes Gerät" } },
            { "per_dispositivo_sotto", new[] { "Sotto che percentuale avvisarti, dispositivo per dispositivo",
                "Below which percentage to warn you, device by device",
                "Ab welchem Prozentsatz du gewarnt wirst, für jedes Gerät" } },
            { "soglia_come_altri", new[] { "Come gli altri", "Like the others", "Wie die anderen" } },
            { "soglia_mai", new[] { "Mai", "Never", "Nie" } },
            { "lingua", new[] { "Lingua", "Language", "Sprache" } },
            { "lingua_sotto", new[] { "Automatica = la stessa lingua di Windows", "Automatic = the same language as Windows", "Automatisch = dieselbe Sprache wie Windows" } },
            { "lingua_auto", new[] { "Automatica", "Automatic", "Automatisch" } },
            { "conferma_lingua", new[] { "Per cambiare lingua l'app deve riavviarsi. Riavvio adesso?",
                "The app needs to restart to change language. Restart now?",
                "Die App muss neu starten, um die Sprache zu ändern. Jetzt neu starten?" } },
            { "titolo_impostazioni", new[] { "Batterie in Barra - Impostazioni", "Batterie in Barra - Settings", "Batterie in Barra - Einstellungen" } },
            { "sezione_dispositivi", new[] { "DISPOSITIVI", "DEVICES", "GERÄTE" } },
            { "sulla_barra", new[] { "Sulla barra", "On taskbar", "In Taskleiste" } },
            { "non_collegato", new[] { "non collegato", "not connected", "nicht verbunden" } },
            { "nessun_dispositivo_ancora", new[] { "Nessun dispositivo ancora. Collega cuffie, tastiera o controller e riapri questa finestra.", "No devices yet. Connect headphones, a keyboard or a controller and reopen this window.", "Noch keine Geräte. Verbinde Kopfhörer, Tastatur oder Controller und öffne dieses Fenster erneut." } },
            { "sposta_su", new[] { "Sposta a sinistra sulla barra", "Move left on the taskbar", "In der Taskleiste nach links" } },
            { "sposta_giu", new[] { "Sposta a destra sulla barra", "Move right on the taskbar", "In der Taskleiste nach rechts" } },

            { "sezione_barra", new[] { "BARRA", "TASKBAR", "TASKLEISTE" } },
            { "mostra_percentuale", new[] { "Mostra la percentuale", "Show percentage", "Prozent anzeigen" } },
            { "mostra_percentuale_sotto", new[] { "Se è spenta vedi solo la batteria: la % compare col mouse", "When off you only see the battery: the % appears on hover", "Wenn aus, siehst du nur den Akku: die % erscheint mit der Maus" } },
            { "animazioni", new[] { "Animazioni", "Animations", "Animationen" } },
            { "animazioni_sotto", new[] { "Dissolvenza quando un dispositivo si collega o si scollega", "Fade when a device connects or disconnects", "Überblenden beim Verbinden oder Trennen eines Geräts" } },

            { "sezione_colori", new[] { "COLORI", "COLOURS", "FARBEN" } },
            { "stile_colori", new[] { "Colore delle batterie", "Battery colour", "Akkufarbe" } },
            { "stile_colori_sotto", new[] { "Bianche: come su Apple, diventano rosse solo quando sono scariche", "White: like on Apple, they only turn red when low", "Weiß: wie bei Apple, nur rot wenn fast leer" } },
            { "a_colori", new[] { "A colori", "Coloured", "Farbig" } },
            { "bianche", new[] { "Bianche", "White", "Weiß" } },
            { "giallo_sotto", new[] { "Gialla sotto il", "Yellow below", "Gelb unter" } },
            { "rosso_sotto", new[] { "Rossa sotto il", "Red below", "Rot unter" } },

            { "sezione_avvisi", new[] { "AVVISI", "NOTIFICATIONS", "BENACHRICHTIGUNGEN" } },
            { "soglia", new[] { "Avviso batteria scarica sotto il", "Low battery alert below", "Warnung bei Akku unter" } },
            { "carica_completa", new[] { "Avvisa quando la carica è completa", "Notify when fully charged", "Melden, wenn voll geladen" } },
            { "carica_completa_sotto", new[] { "Notifica quando un dispositivo torna al 100%", "Notification when a device is back at 100%", "Benachrichtigung, wenn ein Gerät wieder 100% hat" } },
            { "promemoria", new[] { "Promemoria serale", "Evening reminder", "Abend-Erinnerung" } },
            { "promemoria_sotto", new[] { "Se un dispositivo è sotto il 30%, ti ricorda di caricarlo per domani", "If a device is below 30%, reminds you to charge it for tomorrow", "Wenn ein Gerät unter 30% ist, erinnert es dich ans Laden für morgen" } },
            { "ora_promemoria", new[] { "Ora del promemoria", "Reminder time", "Uhrzeit der Erinnerung" } },

            { "non_disturbare", new[] { "Non disturbare", "Do not disturb", "Nicht stören" } },
            { "non_disturbare_sotto", new[] { "Niente avvisi di notte e mentre giochi a schermo intero: arrivano dopo", "No notifications at night or while gaming in full screen: they arrive later", "Keine Benachrichtigungen nachts oder beim Spielen im Vollbild: sie kommen später" } },
            { "silenzio_dalle", new[] { "Silenzio dalle", "Quiet from", "Ruhe ab" } },
            { "silenzio_alle", new[] { "Fino alle", "Until", "Bis" } },
            { "scorciatoia", new[] { "Scorciatoia Ctrl + Alt + B", "Shortcut Ctrl + Alt + B", "Tastenkürzel Strg + Alt + B" } },
            { "scorciatoia_sotto", new[] { "Apre e chiude il widget dalla tastiera", "Opens and closes the widget from the keyboard", "Öffnet und schließt das Widget mit der Tastatur" } },
            { "sezione_generale", new[] { "GENERALE", "GENERAL", "ALLGEMEIN" } },
            { "avvio_windows", new[] { "Avvia con Windows", "Start with Windows", "Mit Windows starten" } },
            { "avvio_windows_sotto", new[] { "Parte da sola quando accendi il PC", "Starts automatically when you turn on the PC", "Startet automatisch beim Einschalten des PCs" } },
            { "batteria_pc", new[] { "Mostra la batteria del PC", "Show the PC battery", "PC-Akku anzeigen" } },
            { "batteria_pc_sotto", new[] { "Nel widget, insieme ai dispositivi", "In the widget, together with the devices", "Im Widget, zusammen mit den Geräten" } },
            { "nessuna_batteria_pc", new[] { "Questo PC non ha una batteria", "This PC has no battery", "Dieser PC hat keinen Akku" } },

            { "sezione_aggiornamenti", new[] { "AGGIORNAMENTI", "UPDATES", "UPDATES" } },
            { "versione", new[] { "Versione {0}", "Version {0}", "Version {0}" } },
            { "controlla_ora", new[] { "Controlla ora", "Check now", "Jetzt prüfen" } },
            { "aggiorna_alla", new[] { "Aggiorna alla {0}", "Update to {0}", "Auf {0} aktualisieren" } },
            { "scarico", new[] { "Scarico…", "Downloading…", "Wird geladen…" } },
            { "controllo", new[] { "Controllo…", "Checking…", "Wird geprüft…" } },
            { "cerca_da_sola", new[] { "Cerca aggiornamenti da sola", "Check for updates automatically", "Automatisch nach Updates suchen" } },
            { "cerca_da_sola_sotto", new[] { "Ogni 6 ore controlla su GitHub. I tuoi dati restano dopo l'aggiornamento", "Checks GitHub every 6 hours. Your data is kept after updating", "Prüft alle 6 Stunden auf GitHub. Deine Daten bleiben nach dem Update erhalten" } },

            { "sezione_dati", new[] { "DATI", "DATA", "DATEN" } },
            { "spazio_occupato", new[] { "Spazio occupato", "Storage used", "Belegter Speicher" } },
            { "spazio_testo", new[] { "Dati raccolti: {0} ({1} letture della batteria)\nProgramma: {2}", "Collected data: {0} ({1} battery readings)\nProgram: {2}", "Gesammelte Daten: {0} ({1} Akku-Messungen)\nProgramm: {2}" } },
            { "supporto", new[] { "Informazioni per il supporto", "Support information", "Support-Informationen" } },
            { "supporto_sotto", new[] { "Copia cosa vede l'app (dispositivi e batterie), da incollare in un messaggio se qualcosa non va", "Copies what the app sees (devices and batteries), to paste in a message if something is wrong", "Kopiert, was die App sieht (Geräte und Akkus), zum Einfügen in eine Nachricht, wenn etwas nicht stimmt" } },
            { "copia", new[] { "Copia", "Copy", "Kopieren" } },
            { "copiato", new[] { "Copiato!", "Copied!", "Kopiert!" } },
            { "apri_cartella", new[] { "Apri cartella", "Open folder", "Ordner öffnen" } },
            { "cancella_cronologia", new[] { "Cancella la cronologia", "Clear history", "Verlauf löschen" } },
            { "cancella_cronologia_sotto", new[] { "Grafici, statistiche e tempo stimato ripartono da zero", "Charts, statistics and estimates start from zero", "Diagramme, Statistiken und Schätzungen beginnen bei null" } },
            { "cancella", new[] { "Cancella", "Clear", "Löschen" } },
            { "ripristina_impostazioni", new[] { "Ripristina le impostazioni", "Reset settings", "Einstellungen zurücksetzen" } },
            { "ripristina_sotto", new[] { "Nomi, ordine, dispositivi nascosti e avvisi tornano come all'inizio", "Names, order, hidden devices and alerts go back to default", "Namen, Reihenfolge, ausgeblendete Geräte und Warnungen werden zurückgesetzt" } },
            { "ripristina", new[] { "Ripristina", "Reset", "Zurücksetzen" } },
            { "elimina_app", new[] { "Elimina l'app dal PC", "Remove the app from this PC", "App vom PC entfernen" } },
            { "elimina_app_sotto", new[] { "Cancella tutti i dati, l'avvio con Windows e il programma", "Deletes all data, autostart and the program", "Löscht alle Daten, den Autostart und das Programm" } },
            { "elimina", new[] { "Elimina", "Remove", "Entfernen" } },
            { "conferma_cronologia", new[] { "Vuoi cancellare la cronologia?\nGrafici, statistiche e tempo stimato ripartiranno da zero.", "Clear the history?\nCharts, statistics and estimates will start from zero.", "Verlauf löschen?\nDiagramme, Statistiken und Schätzungen beginnen bei null." } },
            { "conferma_ripristina", new[] { "Vuoi rimettere le impostazioni di partenza?\nNomi, ordine, dispositivi nascosti e avvisi torneranno come all'inizio.", "Reset to default settings?\nNames, order, hidden devices and alerts will be reset.", "Standardeinstellungen wiederherstellen?\nNamen, Reihenfolge, ausgeblendete Geräte und Warnungen werden zurückgesetzt." } },
            { "conferma_elimina", new[] { "Vuoi eliminare Batterie in Barra dal PC?\n\nVerranno cancellati tutti i dati, l'avvio con Windows e il programma stesso.\nNon si può annullare.", "Remove Batterie in Barra from this PC?\n\nAll data, autostart and the program itself will be deleted.\nThis cannot be undone.", "Batterie in Barra vom PC entfernen?\n\nAlle Daten, der Autostart und das Programm werden gelöscht.\nDas kann nicht rückgängig gemacht werden." } },
        };
    }

    // Permette di scrivere nel XAML: Text="{local:Testo chiave}"
    public class Testo : MarkupExtension
    {
        public string Chiave { get; set; }

        public Testo()
        {
        }

        public Testo(string chiave)
        {
            Chiave = chiave;
        }

        public override object ProvideValue(IServiceProvider servizi)
        {
            return Testi.T(Chiave);
        }
    }
}
