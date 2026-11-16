// ImpostazioniWindow.xaml.cs
// Fa funzionare la finestra Impostazioni.
// Ogni modifica viene salvata subito (non c'è un pulsante "Salva"), come su Apple.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BatterieInBarra
{
    public partial class ImpostazioniWindow : Window
    {
        Impostazioni opzioni;
        bool staCaricando = true;   // mentre mettiamo i valori iniziali, i clic non vanno salvati

        public ImpostazioniWindow()
        {
            InitializeComponent();
            opzioni = App.Corrente.Opzioni;

            PreparaColori();
            SourceInitialized += QuandoCreata;

            // Interruttori: acceso o spento in base alle impostazioni
            MostraPercentuale.IsChecked = opzioni.MostraPercentuale;
            Animazioni.IsChecked = opzioni.Animazioni;
            AvvisoCompleta.IsChecked = opzioni.AvvisoCompleta;
            PromemoriaSerale.IsChecked = opzioni.PromemoriaSerale;
            AvvioWindows.IsChecked = opzioni.AvvioWindows;
            ControllaAggiornamenti.IsChecked = opzioni.ControllaAggiornamenti;
            NonDisturbare.IsChecked = opzioni.NonDisturbare;
            Scorciatoia.IsChecked = opzioni.Scorciatoia;

            // Batteria del PC: solo se il PC ha una batteria (portatile)
            if (App.Corrente.PCHaBatteria())
            {
                MostraBatteriaPC.IsChecked = opzioni.MostraBatteriaPC;
            }
            else
            {
                MostraBatteriaPC.IsChecked = false;
                MostraBatteriaPC.IsEnabled = false;
                SottoBatteriaPC.Text = Testi.T("nessuna_batteria_pc");
            }

            // Le scelte con i bottoni
            Soglia10.IsChecked = opzioni.Soglia == 10;
            Soglia20.IsChecked = opzioni.Soglia == 20;
            Soglia30.IsChecked = opzioni.Soglia == 30;

            StileColori.IsChecked = !opzioni.TuttoBianco;
            StileBianco.IsChecked = opzioni.TuttoBianco;
            Giallo30.IsChecked = opzioni.SogliaGiallo == 30;
            Giallo40.IsChecked = opzioni.SogliaGiallo == 40;
            Giallo50.IsChecked = opzioni.SogliaGiallo == 50;
            Rosso10.IsChecked = opzioni.SogliaRosso == 10;
            Rosso15.IsChecked = opzioni.SogliaRosso == 15;
            Rosso20.IsChecked = opzioni.SogliaRosso == 20;
            AggiornaSceltaGiallo();

            Ora21.IsChecked = opzioni.OraPromemoria == 21;
            Ora22.IsChecked = opzioni.OraPromemoria == 22;
            Ora23.IsChecked = opzioni.OraPromemoria == 23;

            Dalle22.IsChecked = opzioni.SilenzioDalle == 22;
            Dalle23.IsChecked = opzioni.SilenzioDalle == 23;
            Dalle0.IsChecked = opzioni.SilenzioDalle == 0;
            Alle6.IsChecked = opzioni.SilenzioAlle == 6;
            Alle7.IsChecked = opzioni.SilenzioAlle == 7;
            Alle8.IsChecked = opzioni.SilenzioAlle == 8;

            MostraDispositivi();
            MostraAggiornamenti();
            MostraSpazio();
            staCaricando = false;
        }

        // Barra del titolo scura se Windows è scuro
        void QuandoCreata(object sender, EventArgs e)
        {
            Finestre.TitoloScuro(this, !Grafica.AppChiare());
        }


        // ----- Colori della finestra (chiaro o scuro, come Windows) -----

        void PreparaColori()
        {
            if (Grafica.AppChiare())
            {
                Resources["Sfondo"] = Grafica.Pennello("#F2F2F7");
                Resources["SfondoRiquadro"] = Grafica.Pennello("#FFFFFF");
                Resources["SfondoCampo"] = Grafica.Pennello("#F2F2F7");
                Resources["Bordo"] = Grafica.Pennello("#D1D1D6");
                Resources["Testo"] = Grafica.Pennello("#1C1C1E");
                Resources["TestoGrigio"] = Grafica.Pennello("#6E6E73");
                Resources["Linea"] = Grafica.Pennello("#14000000");
                Resources["IconaSfondo"] = Grafica.Pennello("#E5E5EA");
                Resources["Scelto"] = Grafica.Pennello("#FFFFFF");
            }
            else
            {
                Resources["Sfondo"] = Grafica.Pennello("#1E1E20");
                Resources["SfondoRiquadro"] = Grafica.Pennello("#2C2C2E");
                Resources["SfondoCampo"] = Grafica.Pennello("#1C1C1E");
                Resources["Bordo"] = Grafica.Pennello("#48484A");
                Resources["Testo"] = Grafica.Pennello("#FFFFFF");
                Resources["TestoGrigio"] = Grafica.Pennello("#8E8E93");
                Resources["Linea"] = Grafica.Pennello("#14FFFFFF");
                Resources["IconaSfondo"] = Grafica.Pennello("#3A3A3C");
                Resources["Scelto"] = Grafica.Pennello("#636366");
            }
        }

        Brush Colore(string nome)
        {
            return (Brush)Resources[nome];
        }


        // ----- Dispositivi -----

        void MostraDispositivi()
        {
            ListaDispositivi.Children.Clear();

            if (opzioni.Visti.Count == 0)
            {
                TextBlock nessuno = new TextBlock();
                nessuno.Text = Testi.T("nessun_dispositivo_ancora");
                nessuno.Foreground = Colore("TestoGrigio");
                nessuno.FontSize = 13;
                nessuno.TextWrapping = TextWrapping.Wrap;
                nessuno.Margin = new Thickness(0, 12, 0, 12);
                ListaDispositivi.Children.Add(nessuno);
                return;
            }

            for (int i = 0; i < opzioni.Visti.Count; i++)
            {
                if (i > 0)
                {
                    Border separatore = new Border();
                    separatore.Height = 1;
                    separatore.Background = Colore("Linea");
                    separatore.Margin = new Thickness(70, 0, 0, 0);
                    ListaDispositivi.Children.Add(separatore);
                }
                bool primo = i == 0;
                bool ultimo = i == opzioni.Visti.Count - 1;
                ListaDispositivi.Children.Add(CreaRigaDispositivo(opzioni.Visti[i], primo, ultimo));
            }
        }

        FrameworkElement CreaRigaDispositivo(string originale, bool primo, bool ultimo)
        {
            Grid riga = new Grid();
            riga.Margin = new Thickness(0, 11, 0, 11);
            riga.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            riga.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            riga.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            riga.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            riga.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Frecce per l'ordine: ▲ = più a sinistra sulla barra, ▼ = più a destra
            StackPanel frecce = new StackPanel();
            frecce.VerticalAlignment = VerticalAlignment.Center;
            frecce.Margin = new Thickness(0, 0, 4, 0);
            frecce.Children.Add(CreaFreccia("▲", Testi.T("sposta_su"), originale, -1, !primo));
            frecce.Children.Add(CreaFreccia("▼", Testi.T("sposta_giu"), originale, 1, !ultimo));

            // Icona in un quadratino arrotondato
            Border icona = new Border();
            icona.Width = 32;
            icona.Height = 32;
            icona.CornerRadius = new CornerRadius(8);
            icona.Background = Colore("IconaSfondo");
            icona.Margin = new Thickness(0, 0, 12, 0);
            icona.VerticalAlignment = VerticalAlignment.Center;
            FrameworkElement disegno = Grafica.CreaIcona(originale, 18, Colore("Testo"));
            disegno.HorizontalAlignment = HorizontalAlignment.Center;
            disegno.VerticalAlignment = VerticalAlignment.Center;
            icona.Child = disegno;

            // Casella con il nome e, sotto, il nome originale con la batteria
            StackPanel testi = new StackPanel();

            TextBox campo = new TextBox();
            campo.Width = 190;
            campo.HorizontalAlignment = HorizontalAlignment.Left;
            campo.FontSize = 14;
            campo.Padding = new Thickness(5, 3, 5, 3);
            campo.MaxLength = 30;
            campo.Foreground = Colore("Testo");
            campo.Background = Colore("SfondoCampo");
            campo.BorderBrush = Colore("Bordo");
            campo.CaretBrush = Colore("Testo");
            campo.Text = opzioni.NomeDi(originale);
            campo.Tag = originale;   // ci ricordiamo di quale dispositivo è
            campo.LostFocus += QuandoFinisciDiScrivere;
            campo.KeyDown += QuandoPremiTasto;
            testi.Children.Add(campo);

            TextBlock sotto = new TextBlock();
            sotto.FontSize = 11.5;
            sotto.Foreground = Colore("TestoGrigio");
            sotto.Margin = new Thickness(0, 3, 0, 0);
            int batteria = App.Corrente.BatteriaDi(originale);
            if (batteria >= 0)
            {
                sotto.Text = originale + " · " + batteria + "%";
            }
            else
            {
                sotto.Text = originale + " · " + Testi.T("non_collegato");
            }
            testi.Children.Add(sotto);

            // "Sulla barra" e interruttore
            TextBlock etichetta = new TextBlock();
            etichetta.Text = Testi.T("sulla_barra");
            etichetta.FontSize = 12;
            etichetta.Foreground = Colore("TestoGrigio");
            etichetta.VerticalAlignment = VerticalAlignment.Center;
            etichetta.Margin = new Thickness(12, 0, 10, 0);

            CheckBox interruttore = new CheckBox();
            interruttore.Style = (Style)Resources["Interruttore"];
            interruttore.IsChecked = !opzioni.Nascosti.Contains(originale);
            interruttore.Tag = originale;
            interruttore.Click += QuandoCambiaSullaBarra;

            Grid.SetColumn(frecce, 0);
            Grid.SetColumn(icona, 1);
            Grid.SetColumn(testi, 2);
            Grid.SetColumn(etichetta, 3);
            Grid.SetColumn(interruttore, 4);
            riga.Children.Add(frecce);
            riga.Children.Add(icona);
            riga.Children.Add(testi);
            riga.Children.Add(etichetta);
            riga.Children.Add(interruttore);
            return riga;
        }

        Button CreaFreccia(string simbolo, string suggerimento, string originale, int direzione, bool attiva)
        {
            Button freccia = new Button();
            freccia.Style = (Style)Resources["Freccia"];
            freccia.Content = simbolo;
            freccia.ToolTip = suggerimento;
            freccia.IsEnabled = attiva;
            freccia.Click += (s, e) =>
            {
                App.Corrente.SpostaDispositivo(originale, direzione);
                MostraDispositivi();
            };
            return freccia;
        }

        // Quando finisci di scrivere il nome (clic fuori), lo salviamo
        void QuandoFinisciDiScrivere(object sender, RoutedEventArgs e)
        {
            TextBox campo = (TextBox)sender;
            string originale = (string)campo.Tag;
            string nome = campo.Text.Trim();

            // Nome vuoto o uguale all'originale = si torna al nome originale
            if (nome == "" || nome == originale)
            {
                opzioni.Nomi.Remove(originale);
                campo.Text = originale;
            }
            else
            {
                opzioni.Nomi[originale] = nome;
            }
            App.Corrente.ImpostazioniCambiate();
        }

        // Invio = fine scrittura
        void QuandoPremiTasto(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Keyboard.ClearFocus();
                QuandoFinisciDiScrivere(sender, null);
            }
        }

        void QuandoCambiaSullaBarra(object sender, RoutedEventArgs e)
        {
            CheckBox interruttore = (CheckBox)sender;
            string originale = (string)interruttore.Tag;

            opzioni.Nascosti.Remove(originale);
            if (interruttore.IsChecked != true)
            {
                opzioni.Nascosti.Add(originale);
            }
            App.Corrente.ImpostazioniCambiate();
        }


        // ----- Interruttori e scelte -----

        void QuandoCambiaInterruttore(object sender, RoutedEventArgs e)
        {
            if (staCaricando)
            {
                return;
            }
            opzioni.MostraPercentuale = MostraPercentuale.IsChecked == true;
            opzioni.Animazioni = Animazioni.IsChecked == true;
            opzioni.AvvisoCompleta = AvvisoCompleta.IsChecked == true;
            opzioni.PromemoriaSerale = PromemoriaSerale.IsChecked == true;
            opzioni.AvvioWindows = AvvioWindows.IsChecked == true;
            opzioni.ControllaAggiornamenti = ControllaAggiornamenti.IsChecked == true;
            opzioni.NonDisturbare = NonDisturbare.IsChecked == true;
            opzioni.Scorciatoia = Scorciatoia.IsChecked == true;
            if (MostraBatteriaPC.IsEnabled)
            {
                opzioni.MostraBatteriaPC = MostraBatteriaPC.IsChecked == true;
            }
            App.Corrente.ImpostazioniCambiate();
        }

        void QuandoCambiaSoglia(object sender, RoutedEventArgs e)
        {
            if (staCaricando)
            {
                return;
            }
            if (Soglia10.IsChecked == true) opzioni.Soglia = 10;
            if (Soglia20.IsChecked == true) opzioni.Soglia = 20;
            if (Soglia30.IsChecked == true) opzioni.Soglia = 30;
            App.Corrente.SogliaCambiata();
        }

        void QuandoCambiaColori(object sender, RoutedEventArgs e)
        {
            if (staCaricando)
            {
                return;
            }
            opzioni.TuttoBianco = StileBianco.IsChecked == true;

            if (Giallo30.IsChecked == true) opzioni.SogliaGiallo = 30;
            if (Giallo40.IsChecked == true) opzioni.SogliaGiallo = 40;
            if (Giallo50.IsChecked == true) opzioni.SogliaGiallo = 50;

            if (Rosso10.IsChecked == true) opzioni.SogliaRosso = 10;
            if (Rosso15.IsChecked == true) opzioni.SogliaRosso = 15;
            if (Rosso20.IsChecked == true) opzioni.SogliaRosso = 20;

            AggiornaSceltaGiallo();
            App.Corrente.ImpostazioniCambiate();
        }

        // Con le batterie bianche il giallo non c'è: la scelta del giallo diventa grigia
        void AggiornaSceltaGiallo()
        {
            bool attiva = StileBianco.IsChecked != true;
            Giallo30.IsEnabled = attiva;
            Giallo40.IsEnabled = attiva;
            Giallo50.IsEnabled = attiva;
            TitoloGiallo.Opacity = attiva ? 1 : 0.4;
        }

        void QuandoCambiaOra(object sender, RoutedEventArgs e)
        {
            if (staCaricando)
            {
                return;
            }
            if (Ora21.IsChecked == true) opzioni.OraPromemoria = 21;
            if (Ora22.IsChecked == true) opzioni.OraPromemoria = 22;
            if (Ora23.IsChecked == true) opzioni.OraPromemoria = 23;
            opzioni.UltimoPromemoria = "";   // con l'ora nuova il promemoria può arrivare già oggi
            App.Corrente.ImpostazioniCambiate();
        }


        void QuandoCambiaSilenzio(object sender, RoutedEventArgs e)
        {
            if (staCaricando)
            {
                return;
            }
            if (Dalle22.IsChecked == true) opzioni.SilenzioDalle = 22;
            if (Dalle23.IsChecked == true) opzioni.SilenzioDalle = 23;
            if (Dalle0.IsChecked == true) opzioni.SilenzioDalle = 0;
            if (Alle6.IsChecked == true) opzioni.SilenzioAlle = 6;
            if (Alle7.IsChecked == true) opzioni.SilenzioAlle = 7;
            if (Alle8.IsChecked == true) opzioni.SilenzioAlle = 8;
            App.Corrente.ImpostazioniCambiate();
        }


        // ----- Aggiornamenti -----

        void MostraAggiornamenti()
        {
            TestoVersione.Text = Testi.T("versione", Aggiornamenti.VersioneAttuale());
            TestoStatoAggiornamenti.Text = App.Corrente.StatoAggiornamenti;

            if (App.Corrente.VersioneNuova != null)
            {
                PulsanteAggiornamenti.Content = Testi.T("aggiorna_alla", App.Corrente.VersioneNuova.Versione);
            }
            else
            {
                PulsanteAggiornamenti.Content = Testi.T("controlla_ora");
            }
        }

        async void QuandoPulsanteAggiornamenti(object sender, RoutedEventArgs e)
        {
            PulsanteAggiornamenti.IsEnabled = false;

            if (App.Corrente.VersioneNuova != null)
            {
                // C'è una versione nuova: la installiamo (l'app si riavvia da sola)
                PulsanteAggiornamenti.Content = Testi.T("scarico");
                await App.Corrente.InstallaAggiornamento();
            }
            else
            {
                PulsanteAggiornamenti.Content = Testi.T("controllo");
                await App.Corrente.ControllaAggiornamenti();
            }

            MostraAggiornamenti();
            PulsanteAggiornamenti.IsEnabled = true;
        }


        // ----- Dati -----

        void MostraSpazio()
        {
            long dati = App.Corrente.SpazioDati();
            long programma = App.Corrente.SpazioApp();
            int letture = App.Corrente.NumeroLetture();

            TestoSpazio.Text = Testi.T("spazio_testo", Grandezza(dati), letture, Grandezza(programma));
        }

        // Trasforma i byte in un testo leggibile (KB o MB)
        string Grandezza(long byteTotali)
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

        // Copia negli appunti tutto quello che l'app vede, da incollare in un messaggio
        async void QuandoCopiaSupporto(object sender, RoutedEventArgs e)
        {
            PulsanteSupporto.IsEnabled = false;
            string testo = await Batterie.Diagnostica();
            Clipboard.SetText(testo);
            PulsanteSupporto.Content = Testi.T("copiato");
            PulsanteSupporto.IsEnabled = true;
        }

        void QuandoApriCartella(object sender, RoutedEventArgs e)
        {
            App.Corrente.ApriCartellaDati();
        }

        void QuandoCancellaCronologia(object sender, RoutedEventArgs e)
        {
            MessageBoxResult risposta = MessageBox.Show(this, Testi.T("conferma_cronologia"),
                Testi.T("cancella_cronologia"), MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (risposta == MessageBoxResult.Yes)
            {
                App.Corrente.CancellaCronologia();
                MostraSpazio();
            }
        }

        void QuandoRipristina(object sender, RoutedEventArgs e)
        {
            MessageBoxResult risposta = MessageBox.Show(this, Testi.T("conferma_ripristina"),
                Testi.T("ripristina_impostazioni"), MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (risposta == MessageBoxResult.Yes)
            {
                App.Corrente.RipristinaImpostazioni();
                // Riapriamo la finestra per mostrare i valori nuovi
                Close();
                App.Corrente.ApriImpostazioni();
            }
        }

        void QuandoEliminaApp(object sender, RoutedEventArgs e)
        {
            MessageBoxResult risposta = MessageBox.Show(this, Testi.T("conferma_elimina"),
                Testi.T("elimina_app"), MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (risposta == MessageBoxResult.Yes)
            {
                App.Corrente.EliminaApp();
            }
        }
    }
}
