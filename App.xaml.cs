// App.xaml.cs
// Il "cervello" dell'app: legge le batterie, le mette sulla barra di Windows
// (a sinistra delle icone vicino all'orologio), apre il widget e le impostazioni,
// manda gli avvisi e cerca gli aggiornamenti.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace BatterieInBarra
{
    // I dati di un dispositivo pronti da mostrare
    public class Vista
    {
        public string Originale;          // nome originale (o Grafica.NOME_PC per il PC)
        public string Nome;               // nome scelto da te
        public int Batteria;
        public bool SullaBarra;
        public bool InCarica;             // si sta caricando (mostriamo il fulmine)
        public string Tempo;              // "circa 6 ore rimaste"
        public List<Lettura> Storia24;    // ultime 24 ore
        public List<Lettura> Storia7;     // ultimi 7 giorni
        public string DurataCarica;       // "Una carica dura circa 18 ore"
        public string CaricheMese;        // "Caricato 5 volte questo mese"
        public bool BatteriaPeggiorata;   // si scarica più in fretta del solito
        public int Custodia = -1;         // solo AirPods: batteria della custodia (-1 = non si sa)
    }

    // Un avviso rimandato (perché eri in "Non disturbare")
    public class AvvisoInAttesa
    {
        public string Titolo;
        public string Testo;
        public string Dispositivo;
        public DateTime Quando;
    }

    public partial class App : Application
    {
        // ----- Costanti -----
        const int OGNI_QUANTI_SECONDI = 30;              // aggiorna le batterie ogni 30 secondi
        const double LARGHEZZA_CON_PERCENTUALE = 90;     // spazio per dispositivo sulla barra
        const double LARGHEZZA_SENZA_PERCENTUALE = 64;
        const double LARGHEZZA_VUOTA = 44;               // quando non c'è nessun dispositivo
        const int DURATA_DISSOLVENZA = 350;              // millisecondi
        const int SOGLIA_PROMEMORIA = 30;                // promemoria serale sotto questa %

        // ----- Variabili -----
        public Impostazioni Opzioni;
        Cronologia cronologia;
        Statistiche statistiche;
        BarraWindow barra;
        WidgetWindow widget;
        ImpostazioniWindow finestraImpostazioni;
        Mutex unaSolaCopia;
        EventWaitHandle segnaleApri;
        const string NOME_SEGNALE_APRI = "BatterieInBarra_Simone_ApriImpostazioni";

        List<Dispositivo> ultimiDati = new List<Dispositivo>();
        StatoBatteriaPC batteriaPC = null;
        Dictionary<string, int> ultimiValori = new Dictionary<string, int>();
        Dictionary<string, bool> giaAvvisati = new Dictionary<string, bool>();
        Dictionary<string, int> minimoDaUltimaCarica = new Dictionary<string, int>();
        Dictionary<string, bool> inCarica = new Dictionary<string, bool>();
        Dictionary<string, DateTime> ultimaSalita = new Dictionary<string, DateTime>();   // quando la batteria è salita l'ultima volta
        Dictionary<string, bool> perFulmine = new Dictionary<string, bool>();             // la batteria sta salendo (basta +2): mostriamo il fulmine
        double larghezzaBarra = LARGHEZZA_VUOTA;
        DateTime ultimaChiusuraWidget = DateTime.MinValue;
        bool staLeggendo = false;
        bool schermoInteroAdesso = false;                          // c'è un gioco/video a schermo intero?
        List<AvvisoInAttesa> avvisiInAttesa = new List<AvvisoInAttesa>();

        // Per arrivare all'app da tutte le finestre: App.Corrente
        public static App Corrente
        {
            get { return (App)Current; }
        }


        // ----- Avvio -----

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Se succede un errore, non chiudiamoci in silenzio: lo scriviamo nel registro e lo mostriamo
            DispatcherUnhandledException += QuandoErrore;
            AppDomain.CurrentDomain.UnhandledException += QuandoErroreGrave;
            TaskScheduler.UnobservedTaskException += QuandoErroreNascosto;

            // Windows si spegne o fai "Esci" dall'account: ci fermiamo subito e in silenzio
            SessionEnding += QuandoWindowsSiSpegne;

            // I controller Xbox: Windows li elenca solo se qualcuno "ascolta" fin dall'inizio
            ControllerXbox.Prepara();
            Registro.Scrivi("Avvio versione " + Aggiornamenti.VersioneAttuale() + " " + string.Join(" ", e.Args));

            // Siamo appena stati aggiornati? (la versione vecchia ci avvia con "--aggiornato")
            bool dopoAggiornamento = Array.IndexOf(e.Args, "--aggiornato") >= 0;
            // Ci siamo riavviati da soli (es. hai cambiato lingua)?
            bool dopoRiavvio = Array.IndexOf(e.Args, "--riavvio") >= 0;

            // Una sola copia dell'app aperta.
            // Dopo un aggiornamento aspettiamo fino a 10 secondi che la versione vecchia si chiuda.
            unaSolaCopia = new Mutex(false, "BatterieInBarra_Simone");
            bool libero;
            try
            {
                libero = unaSolaCopia.WaitOne(dopoAggiornamento || dopoRiavvio ? 10000 : 0);
            }
            catch (AbandonedMutexException)
            {
                libero = true;   // la copia di prima si è chiusa male: va bene lo stesso
            }
            if (!libero)
            {
                // L'app è già aperta: le chiediamo di aprire le Impostazioni e ci chiudiamo
                try
                {
                    EventWaitHandle.OpenExisting(NOME_SEGNALE_APRI).Set();
                }
                catch (Exception)
                {
                }
                Shutdown();
                return;
            }

            // Se qualcuno riapre l'exe mentre siamo già aperti, apriamo le Impostazioni
            segnaleApri = new EventWaitHandle(false, EventResetMode.AutoReset, NOME_SEGNALE_APRI);
            Thread attesa = new Thread(AspettaRichiesteDiApertura);
            attesa.IsBackground = true;
            attesa.Start();

            // È la prima volta che parte? (non c'è ancora il file delle impostazioni)
            bool primaVolta = !File.Exists(Path.Combine(Impostazioni.Cartella(), "impostazioni.json"));

            Opzioni = Impostazioni.Leggi();
            Opzioni.Salva();
            Testi.ImpostaLingua(Opzioni.Lingua);   // prima di aprire le finestre
            cronologia = new Cronologia();
            statistiche = new Statistiche();
            Avvisi.Prepara();
            Opzioni.ApplicaAvvioWindows();
            ApplicaColori();

            if (primaVolta)
            {
                Avvisi.Manda(Testi.T("avviso_benvenuto"), Testi.T("avviso_benvenuto_testo"));
            }

            if (dopoAggiornamento)
            {
                Avvisi.Manda(Testi.T("avviso_aggiornata"), Testi.T("avviso_aggiornata_testo", Aggiornamenti.VersioneAttuale()));
            }

            barra = new BarraWindow();
            widget = new WidgetWindow();

            // Dopo il riavvio (es. per la lingua) riapriamo le Impostazioni, così vedi subito il cambio
            if (dopoRiavvio)
            {
                Dispatcher.BeginInvoke(new Action(ApriImpostazioni), DispatcherPriority.ApplicationIdle);
            }

            // Clic su una notifica: apre il widget su quel dispositivo
            Avvisi.QuandoCliccata = QuandoCliccaNotifica;

            // Scorciatoia Ctrl + Alt + B
            barra.ImpostaScorciatoia(Opzioni.Scorciatoia);

            // Aggiorna le batterie ogni 30 secondi
            DispatcherTimer timerLettura = new DispatcherTimer();
            timerLettura.Interval = TimeSpan.FromSeconds(OGNI_QUANTI_SECONDI);
            timerLettura.Tick += QuandoTimerLettura;
            timerLettura.Start();
            orologi.Add(timerLettura);

            // Controlla la posizione della barra ogni mezzo secondo
            // (e rimette le batterie davanti, se hai cliccato sulla barra di Windows)
            DispatcherTimer timerPosizione = new DispatcherTimer();
            timerPosizione.Interval = TimeSpan.FromMilliseconds(500);
            timerPosizione.Tick += QuandoTimerPosizione;
            timerPosizione.Start();
            orologi.Add(timerPosizione);

            // Ogni minuto: promemoria serale
            DispatcherTimer timerPromemoria = new DispatcherTimer();
            timerPromemoria.Interval = TimeSpan.FromMinutes(1);
            timerPromemoria.Tick += QuandoTimerPromemoria;
            timerPromemoria.Start();
            orologi.Add(timerPromemoria);

            // Cerca gli aggiornamenti ogni 6 ore
            DispatcherTimer timerAggiornamenti = new DispatcherTimer();
            timerAggiornamenti.Interval = TimeSpan.FromHours(6);
            timerAggiornamenti.Tick += QuandoTimerAggiornamenti;
            timerAggiornamenti.Start();
            orologi.Add(timerAggiornamenti);

            MostraTutto();
            QuandoTimerLettura(null, null);
            PrimoControllo();
        }

        // Errore nelle finestre: lo scriviamo, lo mostriamo e l'app continua
        bool erroreGiaMostrato = false;

        // Diventa true quando Windows si spegne: da lì in poi niente più finestre di errore
        bool staChiudendo = false;

        // I timer (lettura, posizione, promemoria, aggiornamenti): li fermiamo quando ci chiudiamo
        List<DispatcherTimer> orologi = new List<DispatcherTimer>();

        void QuandoErrore(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Registro.Scrivi("ERRORE: " + e.Exception);
            e.Handled = true;

            // Mentre Windows si spegne gli errori sono normali (il Bluetooth si sta fermando):
            // niente finestra, altrimenti Windows aspetta che premi OK e non si spegne
            if (staChiudendo || Dispatcher.HasShutdownStarted)
            {
                return;
            }

            // Il messaggio lo mostriamo una volta sola (se l'errore si ripete non ti riempiamo lo schermo)
            if (erroreGiaMostrato)
            {
                return;
            }
            erroreGiaMostrato = true;
            MessageBox.Show(Testi.T("errore_generico") + "\n\n" + e.Exception.Message,
                "Batterie in Barra", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // Errore dentro un lavoro in sottofondo che nessuno ha controllato: lo scriviamo e basta
        void QuandoErroreNascosto(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Registro.Scrivi("ERRORE (in sottofondo): " + e.Exception);
            e.SetObserved();
        }

        // Windows si sta spegnendo (o stai uscendo dall'account)
        void QuandoWindowsSiSpegne(object sender, SessionEndingCancelEventArgs e)
        {
            Registro.Scrivi("Windows si spegne: chiudo l'app");
            FermaTutto();
            Shutdown();
        }

        // Ferma timer e Bluetooth: dopo non partono più letture
        void FermaTutto()
        {
            staChiudendo = true;
            foreach (DispatcherTimer orologio in orologi)
            {
                orologio.Stop();
            }
            try
            {
                AirPods.Ferma();
            }
            catch (Exception)
            {
            }
        }

        // Errore grave (l'app si chiuderà): almeno lo scriviamo nel registro
        void QuandoErroreGrave(object sender, UnhandledExceptionEventArgs e)
        {
            Registro.Scrivi("ERRORE GRAVE: " + e.ExceptionObject);
        }

        // Gira in sottofondo: quando l'exe viene riaperto, apre le Impostazioni
        void AspettaRichiesteDiApertura()
        {
            while (true)
            {
                segnaleApri.WaitOne();
                try
                {
                    Dispatcher.Invoke(new Action(ApriImpostazioni));
                }
                catch (Exception)
                {
                    return;   // l'app si sta chiudendo
                }
            }
        }

        // 10 secondi dopo l'avvio: togliamo la versione vecchia e cerchiamo aggiornamenti
        async void PrimoControllo()
        {
            await Task.Delay(10000);
            Aggiornamenti.TogliVersioneVecchia();
            QuandoTimerAggiornamenti(null, null);
        }

        async void QuandoTimerLettura(object sender, EventArgs e)
        {
            await Aggiorna();
        }

        void QuandoTimerPosizione(object sender, EventArgs e)
        {
            PosizionaBarra();
        }

        async void QuandoTimerAggiornamenti(object sender, EventArgs e)
        {
            if (Opzioni.ControllaAggiornamenti)
            {
                await ControllaAggiornamenti();
            }
        }

        // I colori scelti nelle impostazioni
        void ApplicaColori()
        {
            Grafica.SogliaGiallo = Opzioni.SogliaGiallo;
            Grafica.SogliaRosso = Opzioni.SogliaRosso;
            Grafica.TuttoBianco = Opzioni.TuttoBianco;
            Grafica.Scelte = Opzioni;   // per le icone scelte da te
        }


        // ----- Aggiornamento delle batterie -----

        public async Task Aggiorna()
        {
            if (staLeggendo || staChiudendo)
            {
                return;
            }
            staLeggendo = true;

            try
            {
                List<Dispositivo> lista = await Batterie.Leggi();
                if (staChiudendo)
                {
                    return;   // Windows si è spento mentre leggevamo
                }
                ultimiDati = lista;

                foreach (Dispositivo d in lista)
                {
                    RicordaDispositivo(d.Nome);
                    NuovaLettura(d.Nome, d.Batteria);
                    ControllaAvvisi(d.Nome, d.Batteria);
                }

                // La batteria del PC (solo sui portatili)
                batteriaPC = BatteriaPC.Leggi();
                if (batteriaPC != null)
                {
                    NuovaLettura(Grafica.NOME_PC, batteriaPC.Percentuale);
                }

                MostraTutto();
            }
            catch (Exception)
            {
                // se Windows non risponde, riproviamo al prossimo giro
            }
            finally
            {
                staLeggendo = false;
            }
        }

        // Salva la lettura e capisce se il dispositivo è appena stato caricato
        void NuovaLettura(string nome, int percentuale)
        {
            Lettura prima = cronologia.UltimaLettura(nome);

            if (prima != null)
            {
                if (!minimoDaUltimaCarica.ContainsKey(nome))
                {
                    minimoDaUltimaCarica[nome] = prima.P;
                }

                if (percentuale < prima.P)
                {
                    // Si sta scaricando
                    inCarica[nome] = false;
                    perFulmine[nome] = false;
                    minimoDaUltimaCarica[nome] = Math.Min(minimoDaUltimaCarica[nome], percentuale);
                }
                else
                {
                    if (percentuale > prima.P)
                    {
                        ultimaSalita[nome] = DateTime.Now;
                    }

                    // Per il fulmine bastano 2 punti in più (così compare prima).
                    // Per le statistiche delle cariche invece ne servono 3 (qui sotto)
                    if (percentuale >= minimoDaUltimaCarica[nome] + 2)
                    {
                        perFulmine[nome] = true;
                    }

                    // È salita di almeno 3 punti rispetto al minimo: è stato caricato
                    bool giaInCarica = inCarica.ContainsKey(nome) && inCarica[nome];
                    if (!giaInCarica && percentuale >= minimoDaUltimaCarica[nome] + 3)
                    {
                        inCarica[nome] = true;
                        Registro.Scrivi("In carica (la batteria sale): " + nome + " " + percentuale + "%");
                        statistiche.RegistraCarica(nome, cronologia.OrePerCaricaCompleta(nome));
                        ControllaSalute(nome);
                    }
                }
            }
            else
            {
                minimoDaUltimaCarica[nome] = percentuale;
            }

            if (inCarica.ContainsKey(nome) && inCarica[nome])
            {
                minimoDaUltimaCarica[nome] = percentuale;
            }

            cronologia.AggiungiLettura(nome, percentuale);
        }

        // Salviamo ogni dispositivo visto, così compare nelle Impostazioni anche quando è spento
        void RicordaDispositivo(string nome)
        {
            if (!Opzioni.Visti.Contains(nome))
            {
                Opzioni.Visti.Add(nome);
                Opzioni.Salva();
            }
        }

        // Prepara i dati da mostrare, nell'ordine scelto nelle Impostazioni
        List<Vista> CreaVista()
        {
            List<Vista> vista = new List<Vista>();

            foreach (string originale in Opzioni.Visti)
            {
                int batteria = BatteriaDi(originale);
                if (batteria < 0)
                {
                    continue;   // non collegato
                }

                Vista v = CreaVistaBase(originale, batteria);
                v.Nome = Opzioni.NomeDi(originale);

                // AirPods e Beats: anche la batteria della custodia
                string minuscolo = originale.ToLower();
                if (minuscolo.Contains("airpods") || minuscolo.Contains("beats"))
                {
                    LetturaAirPods airpods = AirPods.Ultima();
                    if (airpods != null)
                    {
                        v.Custodia = airpods.Custodia;
                    }
                }
                v.SullaBarra = !Opzioni.Nascosti.Contains(originale);
                v.InCarica = StaCaricando(originale);
                if (v.InCarica)
                {
                    v.Tempo = Testi.T("in_carica");
                }
                else
                {
                    v.Tempo = cronologia.TempoStimato(originale);
                }
                vista.Add(v);
            }

            // La batteria del PC va solo nel widget
            if (batteriaPC != null && Opzioni.MostraBatteriaPC)
            {
                Vista pc = CreaVistaBase(Grafica.NOME_PC, batteriaPC.Percentuale);
                pc.Nome = Testi.T("questo_pc");
                pc.SullaBarra = false;
                pc.InCarica = batteriaPC.InCarica;
                if (batteriaPC.InCarica)
                {
                    pc.Tempo = Testi.T("in_carica");
                }
                else if (batteriaPC.SecondiRimasti > 0)
                {
                    pc.Tempo = Cronologia.TestoTempoRimasto(batteriaPC.SecondiRimasti / 3600.0);
                }
                else
                {
                    pc.Tempo = cronologia.TempoStimato(Grafica.NOME_PC);
                }
                vista.Add(pc);
            }
            return vista;
        }

        // Le parti uguali per dispositivi e PC: cronologia e statistiche
        Vista CreaVistaBase(string originale, int batteria)
        {
            Vista v = new Vista();
            v.Originale = originale;
            v.Batteria = batteria;
            v.Storia24 = cronologia.UltimeOre(originale, 24);
            v.Storia7 = cronologia.UltimeOre(originale, 24 * 7);

            double media = statistiche.DurataMedia(originale);
            if (media > 0)
            {
                v.DurataCarica = Testi.T("dura_carica", Cronologia.TestoDurata(media));
            }
            else
            {
                v.DurataCarica = Testi.T("durata_da_calcolare");
            }
            v.CaricheMese = Testi.T("cariche_mese", statistiche.CaricheQuestoMese(originale));
            v.BatteriaPeggiorata = statistiche.BatteriaPeggiorata(originale);
            return v;
        }

        // Manda i dati alla barra e al widget
        public void MostraTutto()
        {
            ApplicaColori();
            List<Vista> vista = CreaVista();

            // Sulla barra solo quelli non nascosti
            List<Vista> perLaBarra = new List<Vista>();
            foreach (Vista v in vista)
            {
                if (v.SullaBarra)
                {
                    perLaBarra.Add(v);
                }
            }

            barra.Mostra(perLaBarra, Opzioni.MostraPercentuale, Opzioni.Animazioni);
            widget.Mostra(vista);
            AggiornaLarghezza(perLaBarra.Count);
        }


        // ----- Avvisi -----

        void ControllaAvvisi(string originale, int percentuale)
        {
            string nome = Opzioni.NomeDi(originale);

            // Batteria scarica (una volta sola, finché non torna sopra la soglia)
            bool avvisato = giaAvvisati.ContainsKey(originale) && giaAvvisati[originale];
            if (percentuale <= Opzioni.Soglia && !avvisato)
            {
                MandaAvviso(Testi.T("avviso_scarica"), Testi.T("avviso_scarica_testo", nome, percentuale), originale, false);
                giaAvvisati[originale] = true;
            }
            if (percentuale > Opzioni.Soglia)
            {
                giaAvvisati[originale] = false;
            }

            // Carica completa: prima era sotto 100, adesso è 100
            if (Opzioni.AvvisoCompleta && ultimiValori.ContainsKey(originale) && ultimiValori[originale] < 100 && percentuale == 100)
            {
                MandaAvviso(Testi.T("avviso_completa"), Testi.T("avviso_completa_testo", nome), originale, false);
            }
            ultimiValori[originale] = percentuale;
        }

        // Dopo ogni carica: la batteria si scarica molto più in fretta di prima?
        void ControllaSalute(string originale)
        {
            if (statistiche.BatteriaPeggiorata(originale) && statistiche.PuoiAvvisareSalute(originale))
            {
                string nome = originale == Grafica.NOME_PC ? Testi.T("questo_pc") : Opzioni.NomeDi(originale);
                MandaAvviso(Testi.T("avviso_salute"), Testi.T("avviso_salute_testo", nome), originale, false);
            }
        }

        // Promemoria serale: all'ora scelta, una volta al giorno
        void QuandoTimerPromemoria(object sender, EventArgs e)
        {
            SvuotaAvvisiInAttesa();

            DateTime adesso = DateTime.Now;
            string oggi = adesso.ToString("yyyy-MM-dd");

            if (!Opzioni.PromemoriaSerale || adesso.Hour != Opzioni.OraPromemoria || Opzioni.UltimoPromemoria == oggi)
            {
                return;
            }
            Opzioni.UltimoPromemoria = oggi;
            Opzioni.Salva();

            // Tutti i dispositivi letti nelle ultime 24 ore (anche se adesso sono spenti)
            long unGiornoFa = Cronologia.Adesso() - 24 * Cronologia.UN_ORA;
            foreach (string originale in Opzioni.Visti)
            {
                Lettura ultima = cronologia.UltimaLettura(originale);
                if (ultima != null && ultima.T > unGiornoFa && ultima.P < SOGLIA_PROMEMORIA)
                {
                    // Il promemoria arriva all'ora che hai scelto anche in "Non disturbare" (ma non mentre giochi)
                    MandaAvviso(Testi.T("avviso_promemoria"),
                        Testi.T("avviso_promemoria_testo", Opzioni.NomeDi(originale), ultima.P), originale, true);
                }
            }
        }


        // ----- Non disturbare -----

        // Siamo in un momento in cui non disturbare? (di notte o mentre giochi)
        bool MomentoSilenzioso(bool ignoraOrario)
        {
            if (!Opzioni.NonDisturbare)
            {
                return false;
            }
            if (schermoInteroAdesso)
            {
                return true;
            }
            if (ignoraOrario)
            {
                return false;
            }

            return Impostazioni.OraSilenziosa(DateTime.Now.Hour, Opzioni.SilenzioDalle, Opzioni.SilenzioAlle);
        }

        // Manda un avviso adesso, oppure lo tiene da parte se è un momento silenzioso
        void MandaAvviso(string titolo, string testo, string dispositivo, bool ignoraOrario)
        {
            if (!MomentoSilenzioso(ignoraOrario))
            {
                Avvisi.Manda(titolo, testo, dispositivo);
                return;
            }

            // Lo stesso avviso non lo teniamo due volte
            foreach (AvvisoInAttesa a in avvisiInAttesa)
            {
                if (a.Titolo == titolo && a.Dispositivo == dispositivo)
                {
                    a.Testo = testo;
                    return;
                }
            }

            AvvisoInAttesa nuovo = new AvvisoInAttesa();
            nuovo.Titolo = titolo;
            nuovo.Testo = testo;
            nuovo.Dispositivo = dispositivo;
            nuovo.Quando = DateTime.Now;
            avvisiInAttesa.Add(nuovo);
        }

        // Finito il momento silenzioso, mandiamo gli avvisi tenuti da parte (se hanno meno di 12 ore)
        void SvuotaAvvisiInAttesa()
        {
            if (avvisiInAttesa.Count == 0 || MomentoSilenzioso(false))
            {
                return;
            }

            foreach (AvvisoInAttesa a in avvisiInAttesa)
            {
                if ((DateTime.Now - a.Quando).TotalHours < 12)
                {
                    Avvisi.Manda(a.Titolo, a.Testo, a.Dispositivo);
                }
            }
            avvisiInAttesa.Clear();
        }

        // Hai cliccato una notifica: apriamo il widget su quel dispositivo
        // (Windows ci chiama da un altro "thread", quindi passiamo da Dispatcher)
        void QuandoCliccaNotifica(string dispositivo)
        {
            Dispatcher.Invoke(new Action(() => ApriWidgetSu(dispositivo)));
        }

        void ApriWidgetSu(string dispositivo)
        {
            widget.ScegliDispositivo(dispositivo);
            if (widget.IsVisible)
            {
                widget.Activate();
            }
            else
            {
                ultimaChiusuraWidget = DateTime.MinValue;
                ApriChiudiWidget();
            }
        }


        // ----- Posizione sulla barra di Windows -----

        double CalcolaLarghezza(int quanti)
        {
            if (quanti == 0)
            {
                return LARGHEZZA_VUOTA;
            }
            if (Opzioni.MostraPercentuale)
            {
                return quanti * LARGHEZZA_CON_PERCENTUALE + 4;
            }
            return quanti * LARGHEZZA_SENZA_PERCENTUALE + 4;
        }

        async void AggiornaLarghezza(int quanti)
        {
            double nuova = CalcolaLarghezza(quanti);

            // Se la barra si stringe, aspettiamo la fine della dissolvenza
            if (nuova < larghezzaBarra && Opzioni.Animazioni)
            {
                await Task.Delay(DURATA_DISSOLVENZA);
            }
            larghezzaBarra = nuova;
            PosizionaBarra();
        }

        void PosizionaBarra()
        {
            InfoBarra info = Finestre.LeggiBarraWindows();
            if (info == null)
            {
                return;
            }

            // Gioco o video a schermo intero: nascondiamo le batterie (e gli avvisi aspettano)
            schermoInteroAdesso = info.SchermoIntero;
            if (info.SchermoIntero)
            {
                if (barra.IsVisible)
                {
                    barra.Hide();
                }
                return;
            }

            if (!barra.IsVisible)
            {
                barra.Show();
            }

            // Da pixel "con lo zoom" a pixel veri
            int larghezza = (int)Math.Round(larghezzaBarra * info.Scala);
            int x = info.InizioIcone - larghezza - (int)Math.Round((4 + Opzioni.Spostamento) * info.Scala);

            Finestre.MettiDavanti(barra, x, info.Alto, larghezza, info.Altezza);
        }

        public void CambiaSpostamento(int quanto)
        {
            Opzioni.Spostamento = Opzioni.Spostamento + quanto;
            Opzioni.Salva();
            PosizionaBarra();
        }


        // ----- Il widget -----

        public void ApriChiudiWidget()
        {
            // Se si è appena chiuso (perché hai cliccato sulle batterie), non lo riapriamo
            if ((DateTime.Now - ultimaChiusuraWidget).TotalMilliseconds < 300)
            {
                return;
            }

            if (widget.IsVisible)
            {
                widget.Hide();
                return;
            }

            // Lo apriamo invisibile per sapere quanto è alto, poi lo spostiamo e lo mostriamo
            widget.Opacity = 0;
            widget.Show();
            widget.UpdateLayout();

            double scala = Finestre.Scala(widget);
            Finestre.RECT posBarra = Finestre.Posizione(barra);
            Rect schermo = SystemParameters.WorkArea;

            double x = posBarra.Right / scala - widget.ActualWidth + 10;
            if (x + widget.ActualWidth > schermo.Right)
            {
                x = schermo.Right - widget.ActualWidth - 8;
            }
            if (x < schermo.Left)
            {
                x = schermo.Left + 8;
            }

            // Se la barra di Windows è in basso il widget va sopra, se è in alto va sotto
            double y = posBarra.Top / scala - widget.ActualHeight;
            if (posBarra.Top / scala < schermo.Top + schermo.Height / 2)
            {
                y = posBarra.Bottom / scala;
            }

            widget.Left = x;
            widget.Top = y;
            widget.Opacity = 1;
            widget.Activate();
        }

        public void WidgetChiuso()
        {
            ultimaChiusuraWidget = DateTime.Now;
        }


        // ----- La finestra Impostazioni -----

        public void ApriImpostazioni()
        {
            if (finestraImpostazioni != null)
            {
                finestraImpostazioni.Activate();
                return;
            }

            finestraImpostazioni = new ImpostazioniWindow();
            finestraImpostazioni.Closed += QuandoImpostazioniChiuse;
            finestraImpostazioni.Show();
            finestraImpostazioni.Activate();
        }

        void QuandoImpostazioniChiuse(object sender, EventArgs e)
        {
            finestraImpostazioni = null;
        }

        // La batteria attuale di un dispositivo (-1 se non è collegato)
        // Il dispositivo si sta caricando?
        // - controller PlayStation e AirPods ce lo dicono loro
        // - gli altri no: lo capiamo se la batteria è salita (di almeno 2 punti) e l'ultima salita è di meno di un'ora fa.
        //   Un'ora perché molte cuffie mandano la batteria a scatti di 10% (uno scatto ogni 15-30 minuti)
        bool StaCaricando(string originale)
        {
            foreach (Dispositivo d in ultimiDati)
            {
                if (d.Nome == originale && d.SaSeCarica)
                {
                    return d.InCarica;
                }
            }

            bool salita = perFulmine.ContainsKey(originale) && perFulmine[originale];
            bool salitaDaPoco = ultimaSalita.ContainsKey(originale)
                && DateTime.Now - ultimaSalita[originale] < TimeSpan.FromMinutes(60);
            return salita && salitaDaPoco && BatteriaDi(originale) < 100;
        }

        public int BatteriaDi(string originale)
        {
            foreach (Dispositivo d in ultimiDati)
            {
                if (d.Nome == originale)
                {
                    return d.Batteria;
                }
            }
            return -1;
        }

        // Il PC ha una batteria? (per le Impostazioni)
        public bool PCHaBatteria()
        {
            return BatteriaPC.CeLaBatteria();
        }

        // Le Impostazioni chiamano questo dopo ogni modifica
        public void ImpostazioniCambiate()
        {
            Opzioni.Salva();
            Opzioni.ApplicaAvvioWindows();
            barra.ImpostaScorciatoia(Opzioni.Scorciatoia);
            MostraTutto();
        }

        // La soglia è cambiata: gli avvisi ripartono da zero
        public void SogliaCambiata()
        {
            giaAvvisati.Clear();
            ImpostazioniCambiate();
        }

        // Sposta un dispositivo nell'ordine (-1 = a sinistra, +1 = a destra)
        public void SpostaDispositivo(string originale, int direzione)
        {
            int posizione = Opzioni.Visti.IndexOf(originale);
            int nuovaPosizione = posizione + direzione;
            if (posizione < 0 || nuovaPosizione < 0 || nuovaPosizione >= Opzioni.Visti.Count)
            {
                return;
            }

            // Scambiamo i due dispositivi
            string altro = Opzioni.Visti[nuovaPosizione];
            Opzioni.Visti[nuovaPosizione] = originale;
            Opzioni.Visti[posizione] = altro;
            ImpostazioniCambiate();
        }


        // ----- Aggiornamenti dell'app -----

        // La versione nuova trovata (null = nessuna)
        public NuovaVersione VersioneNuova;
        // Il risultato dell'ultimo controllo, da mostrare nelle Impostazioni
        public string StatoAggiornamenti = Testi.T("stato_mai");
        string versioneGiaAvvisata = "";

        public async Task ControllaAggiornamenti()
        {
            try
            {
                VersioneNuova = await Aggiornamenti.Controlla();
                if (VersioneNuova == null)
                {
                    StatoAggiornamenti = Testi.T("stato_ultima");
                }
                else
                {
                    StatoAggiornamenti = Testi.T("stato_disponibile", VersioneNuova.Versione);

                    // Avviso una volta sola per ogni versione nuova
                    if (versioneGiaAvvisata != VersioneNuova.Versione)
                    {
                        MandaAvviso(Testi.T("avviso_aggiornamento"), Testi.T("avviso_aggiornamento_testo", VersioneNuova.Versione), null, false);
                        versioneGiaAvvisata = VersioneNuova.Versione;
                    }
                }
            }
            catch (Exception)
            {
                StatoAggiornamenti = Testi.T("stato_errore");
            }

            barra.MostraAggiornamento(VersioneNuova);
        }

        // Scarica la versione nuova; poi uno script la mette al posto di questa (ad app chiusa) e la riavvia
        public async Task InstallaAggiornamento()
        {
            if (VersioneNuova == null)
            {
                return;
            }

            // La finestrella con la barra di avanzamento
            AggiornamentoWindow finestra = new AggiornamentoWindow(VersioneNuova.Versione);
            finestra.Show();
            finestra.Activate();

            try
            {
                Registro.Scrivi("Aggiornamento: scarico la versione " + VersioneNuova.Versione);
                string scaricato = await Aggiornamenti.Scarica(VersioneNuova, finestra.MostraAvanzamento, finestra.Annullamento);
                Registro.Scrivi("Aggiornamento: scaricato, avvio il cambio");

                finestra.MostraInstallazione();
                await Task.Delay(800);   // giusto il tempo di leggere "Installo e riavvio l'app…"
                Aggiornamenti.PreparaCambio(scaricato);
            }
            catch (OperationCanceledException) when (finestra.Annullamento.IsCancellationRequested)
            {
                // Hai premuto "Annulla" (o chiuso la finestrella): non succede niente, l'app resta com'è
                Registro.Scrivi("Aggiornamento annullato");
                if (finestra.IsVisible)
                {
                    finestra.Close();
                }
                return;
            }
            catch (Exception errore)
            {
                // Qualcosa è andato storto (anche internet troppo lento)
                Registro.Scrivi("Aggiornamento non riuscito: " + errore);
                if (finestra.IsVisible)
                {
                    finestra.Close();
                }

                // Proponiamo di scaricarla a mano dalla pagina della release
                MessageBoxResult risposta = MessageBox.Show(
                    Testi.T("errore_aggiornamento") + "\n\n" + errore.Message + "\n\n" + Testi.T("apri_pagina_release"),
                    "Batterie in Barra", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (risposta == MessageBoxResult.Yes)
                {
                    ProcessStartInfo pagina = new ProcessStartInfo(Aggiornamenti.PaginaRelease());
                    pagina.UseShellExecute = true;
                    Process.Start(pagina);
                }
                return;
            }

            // Ci chiudiamo: lo script aspetta questo momento per fare il cambio
            Shutdown();
        }


        // ----- Dati raccolti -----

        // Quanto spazio occupano i dati (in byte)
        public long SpazioDati()
        {
            long totale = 0;
            foreach (string file in Directory.GetFiles(Impostazioni.Cartella()))
            {
                totale = totale + new FileInfo(file).Length;
            }
            return totale;
        }

        // Quanto spazio occupa l'app (il file .exe)
        public long SpazioApp()
        {
            return new FileInfo(Environment.ProcessPath).Length;
        }

        public int NumeroLetture()
        {
            return cronologia.NumeroLetture();
        }

        public void ApriCartellaDati()
        {
            ProcessStartInfo apri = new ProcessStartInfo("explorer.exe", "\"" + Impostazioni.Cartella() + "\"");
            apri.UseShellExecute = true;
            Process.Start(apri);
        }

        // Cancella grafici, statistiche e tempo stimato
        public void CancellaCronologia()
        {
            cronologia.Cancella();
            statistiche.Cancella();
            minimoDaUltimaCarica.Clear();
            inCarica.Clear();
            ultimaSalita.Clear();
            perFulmine.Clear();
            MostraTutto();
        }

        // Rimette le impostazioni di partenza (i dispositivi visti restano, così compaiono ancora nella lista)
        public void RipristinaImpostazioni()
        {
            List<string> visti = Opzioni.Visti;

            // Le icone caricate da te non servono più
            try
            {
                string icone = System.IO.Path.Combine(Impostazioni.Cartella(), "icone");
                if (System.IO.Directory.Exists(icone))
                {
                    System.IO.Directory.Delete(icone, true);
                }
            }
            catch (Exception)
            {
            }

            Opzioni = new Impostazioni();
            Opzioni.Visti = visti;
            Opzioni.Salva();
            Opzioni.ApplicaAvvioWindows();
            giaAvvisati.Clear();
            MostraTutto();
        }

        // Elimina l'app dal PC: dati, avvio con Windows, notifiche e il file .exe
        public void EliminaApp()
        {
            // 1. Non parte più con Windows e non usa più le notifiche
            Opzioni.AvvioWindows = false;
            Opzioni.ApplicaAvvioWindows();
            Avvisi.Togli();

            // 2. Cancella la cartella dei dati
            try
            {
                Directory.Delete(Impostazioni.Cartella(), true);
            }
            catch (Exception)
            {
            }

            // 3. Il programma acceso non si può cancellare da solo:
            //    chiediamo a Windows di cancellarlo 2 secondi dopo la chiusura.
            //    Se è stata installata con l'installer, usiamo il suo disinstallatore
            //    (così sparisce anche da "App installate").
            string disinstallatore = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "unins000.exe");
            string comando = "/c ping -n 3 127.0.0.1 > nul & del /f /q \"" + Environment.ProcessPath + "\"";
            if (File.Exists(disinstallatore))
            {
                comando = "/c ping -n 3 127.0.0.1 > nul & \"" + disinstallatore + "\" /VERYSILENT /SUPPRESSMSGBOXES";
            }
            ProcessStartInfo cancella = new ProcessStartInfo("cmd.exe", comando);
            cancella.CreateNoWindow = true;
            cancella.UseShellExecute = false;
            Process.Start(cancella);

            Shutdown();
        }


        // ----- Uscita -----

        // Chiude l'app e la riapre (con le Impostazioni aperte), es. dopo aver cambiato lingua
        public void Riavvia()
        {
            try
            {
                Process.Start(Environment.ProcessPath, "--riavvio");
            }
            catch (Exception errore)
            {
                Registro.Scrivi("Riavvio non riuscito: " + errore.Message);
                return;
            }
            Esci();
        }

        public void Esci()
        {
            FermaTutto();
            Shutdown();
        }
    }
}
