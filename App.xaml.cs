// App.xaml.cs
// Il "cervello" dell'app: legge le batterie, le mette sulla barra di Windows
// (a sinistra delle icone vicino all'orologio), apre il widget e le impostazioni.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace BatterieInBarra
{
    // I dati di un dispositivo pronti da mostrare (con il nome scelto da te, il tempo e la cronologia)
    public class Vista
    {
        public string Originale;
        public string Nome;
        public int Batteria;
        public bool SullaBarra;
        public string Tempo;
        public List<Lettura> Storia;
    }

    public partial class App : Application
    {
        // ----- Costanti -----
        const int OGNI_QUANTI_SECONDI = 30;              // aggiorna le batterie ogni 30 secondi
        const double LARGHEZZA_CON_PERCENTUALE = 90;     // spazio per dispositivo sulla barra
        const double LARGHEZZA_SENZA_PERCENTUALE = 64;
        const double LARGHEZZA_VUOTA = 44;               // quando non c'è nessun dispositivo
        const int DURATA_DISSOLVENZA = 350;              // millisecondi

        // ----- Variabili -----
        public Impostazioni Opzioni;
        Cronologia cronologia;
        BarraWindow barra;
        WidgetWindow widget;
        ImpostazioniWindow finestraImpostazioni;
        Mutex unaSolaCopia;

        List<Dispositivo> ultimiDati = new List<Dispositivo>();
        Dictionary<string, int> ultimiValori = new Dictionary<string, int>();
        Dictionary<string, bool> giaAvvisati = new Dictionary<string, bool>();
        double larghezzaBarra = LARGHEZZA_VUOTA;
        DateTime ultimaChiusuraWidget = DateTime.MinValue;
        bool staLeggendo = false;

        // Per arrivare all'app da tutte le finestre: App.Corrente
        public static App Corrente
        {
            get { return (App)Current; }
        }


        // ----- Avvio -----

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Una sola copia dell'app aperta
            bool primaCopia;
            unaSolaCopia = new Mutex(true, "BatterieInBarra_Simone", out primaCopia);
            if (!primaCopia)
            {
                Shutdown();
                return;
            }

            Opzioni = Impostazioni.Leggi();
            cronologia = new Cronologia();
            Avvisi.Prepara();
            Opzioni.ApplicaAvvioWindows();

            barra = new BarraWindow();
            widget = new WidgetWindow();

            // Aggiorna le batterie ogni 30 secondi
            DispatcherTimer timerLettura = new DispatcherTimer();
            timerLettura.Interval = TimeSpan.FromSeconds(OGNI_QUANTI_SECONDI);
            timerLettura.Tick += QuandoTimerLettura;
            timerLettura.Start();

            // Controlla la posizione della barra ogni mezzo secondo
            // (e rimette le batterie davanti, se hai cliccato sulla barra di Windows)
            DispatcherTimer timerPosizione = new DispatcherTimer();
            timerPosizione.Interval = TimeSpan.FromMilliseconds(500);
            timerPosizione.Tick += QuandoTimerPosizione;
            timerPosizione.Start();

            MostraTutto();
            QuandoTimerLettura(null, null);
        }

        async void QuandoTimerLettura(object sender, EventArgs e)
        {
            await Aggiorna();
        }

        void QuandoTimerPosizione(object sender, EventArgs e)
        {
            PosizionaBarra();
        }


        // ----- Aggiornamento -----

        public async Task Aggiorna()
        {
            if (staLeggendo)
            {
                return;
            }
            staLeggendo = true;

            try
            {
                List<Dispositivo> lista = await Batterie.Leggi();
                ultimiDati = lista;

                foreach (Dispositivo d in lista)
                {
                    RicordaDispositivo(d.Nome);
                    cronologia.AggiungiLettura(d.Nome, d.Batteria);
                    ControllaAvvisi(d.Nome, d.Batteria);
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

        // Salviamo ogni dispositivo visto, così compare nelle Impostazioni anche quando è spento
        void RicordaDispositivo(string nome)
        {
            if (!Opzioni.Visti.Contains(nome))
            {
                Opzioni.Visti.Add(nome);
                Opzioni.Salva();
            }
        }

        // Prepara i dati da mostrare
        List<Vista> CreaVista()
        {
            List<Vista> vista = new List<Vista>();

            foreach (Dispositivo d in ultimiDati)
            {
                Vista v = new Vista();
                v.Originale = d.Nome;
                v.Nome = Opzioni.NomeDi(d.Nome);
                v.Batteria = d.Batteria;
                v.SullaBarra = !Opzioni.Nascosti.Contains(d.Nome);
                v.Tempo = cronologia.TempoStimato(d.Nome);
                v.Storia = cronologia.Ultime24Ore(d.Nome);
                vista.Add(v);
            }
            return vista;
        }

        // Manda i dati alla barra e al widget
        public void MostraTutto()
        {
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
                Avvisi.Manda("Batteria scarica", nome + " è al " + percentuale + "%. Mettilo in carica!");
                giaAvvisati[originale] = true;
            }
            if (percentuale > Opzioni.Soglia)
            {
                giaAvvisati[originale] = false;
            }

            // Carica completa: prima era sotto 100, adesso è 100
            if (Opzioni.AvvisoCompleta && ultimiValori.ContainsKey(originale) && ultimiValori[originale] < 100 && percentuale == 100)
            {
                Avvisi.Manda("Carica completa", nome + " è carico al 100%. Puoi staccarlo!");
            }
            ultimiValori[originale] = percentuale;
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

            // Gioco o video a schermo intero: nascondiamo le batterie
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

        // Le Impostazioni chiamano questo dopo ogni modifica
        public void ImpostazioniCambiate()
        {
            Opzioni.Salva();
            Opzioni.ApplicaAvvioWindows();
            MostraTutto();
        }

        // La soglia è cambiata: gli avvisi ripartono da zero
        public void SogliaCambiata()
        {
            giaAvvisati.Clear();
            ImpostazioniCambiate();
        }


        // ----- Uscita -----

        public void Esci()
        {
            Shutdown();
        }
    }
}
