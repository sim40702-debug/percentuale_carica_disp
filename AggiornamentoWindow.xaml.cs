// AggiornamentoWindow.xaml.cs
// La finestrella con la barra di avanzamento mentre si scarica la versione nuova.

using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;

namespace BatterieInBarra
{
    public partial class AggiornamentoWindow : Window
    {
        // Serve per fermare lo scaricamento se premi "Annulla" (o chiudi la finestra)
        CancellationTokenSource stop = new CancellationTokenSource();
        bool finito = false;

        public AggiornamentoWindow(string versione)
        {
            InitializeComponent();
            Titolo.Text = Testi.T("agg_titolo", versione);
            PreparaColori();
            SourceInitialized += QuandoCreata;
            Closing += QuandoSiChiude;
        }

        // App lo passa allo scaricamento: quando premi "Annulla" lo scaricamento si ferma
        public CancellationToken Annullamento
        {
            get { return stop.Token; }
        }

        void QuandoCreata(object sender, EventArgs e)
        {
            Finestre.TitoloScuro(this, !Grafica.AppChiare());
        }

        // Colori chiari o scuri, come Windows
        void PreparaColori()
        {
            if (Grafica.AppChiare())
            {
                Background = Grafica.Pennello("#F2F2F7");
                Titolo.Foreground = Grafica.Pennello("#1C1C1E");
                Stato.Foreground = Grafica.Pennello("#6E6E73");
                Avanzamento.Foreground = Grafica.Pennello("#6E6E73");
                Binario.Background = Grafica.Pennello("#1A000000");
            }
            else
            {
                Background = Grafica.Pennello("#1E1E20");
                Titolo.Foreground = Grafica.Pennello("#FFFFFF");
                Stato.Foreground = Grafica.Pennello("#8E8E93");
                Avanzamento.Foreground = Grafica.Pennello("#8E8E93");
                Binario.Background = Grafica.Pennello("#2C2C2E");
            }
        }

        // La chiama lo scaricamento: "scaricati" byte su "totale" (totale -1 = non si sa)
        public void MostraAvanzamento(long scaricati, long totale)
        {
            if (totale > 0)
            {
                double parte = (double)scaricati / totale;
                Riempimento.Width = Binario.ActualWidth * parte;
                int percentuale = (int)(parte * 100);
                Avanzamento.Text = Testi.T("agg_avanzamento", Testi.Grandezza(scaricati), Testi.Grandezza(totale), percentuale);
            }
            else
            {
                Avanzamento.Text = Testi.T("agg_avanzamento_senza_totale", Testi.Grandezza(scaricati));
            }
        }

        // Scaricamento finito: adesso l'app si chiude e riparte aggiornata
        public void MostraInstallazione()
        {
            finito = true;
            Riempimento.Width = Binario.ActualWidth;
            Stato.Text = Testi.T("agg_installo");
            PulsanteAnnulla.IsEnabled = false;
        }

        void QuandoAnnulla(object sender, RoutedEventArgs e)
        {
            stop.Cancel();
        }

        // Chiudere la finestra con la X = annullare
        void QuandoSiChiude(object sender, CancelEventArgs e)
        {
            if (!finito)
            {
                stop.Cancel();
            }
        }
    }
}
