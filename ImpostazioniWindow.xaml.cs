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
            AvvioWindows.IsChecked = opzioni.AvvioWindows;

            // Soglia: segniamo il bottone giusto
            Soglia10.IsChecked = opzioni.Soglia == 10;
            Soglia20.IsChecked = opzioni.Soglia == 20;
            Soglia30.IsChecked = opzioni.Soglia == 30;

            MostraDispositivi();
        }

        // Barra del titolo scura se Windows è scuro
        void QuandoCreata(object sender, EventArgs e)
        {
            Finestre.TitoloScuro(this, !Grafica.AppChiare());
        }


        // ----- Colori (chiaro o scuro, come Windows) -----

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
                nessuno.Text = "Nessun dispositivo ancora. Collega cuffie, tastiera o controller e riapri questa finestra.";
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
                    separatore.Margin = new Thickness(44, 0, 0, 0);
                    ListaDispositivi.Children.Add(separatore);
                }
                ListaDispositivi.Children.Add(CreaRigaDispositivo(opzioni.Visti[i]));
            }
        }

        FrameworkElement CreaRigaDispositivo(string originale)
        {
            Grid riga = new Grid();
            riga.Margin = new Thickness(0, 11, 0, 11);
            riga.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            riga.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            riga.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            riga.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

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
            campo.Width = 210;
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
                sotto.Text = originale + " · non collegato";
            }
            testi.Children.Add(sotto);

            // "Sulla barra" e interruttore
            TextBlock etichetta = new TextBlock();
            etichetta.Text = "Sulla barra";
            etichetta.FontSize = 12;
            etichetta.Foreground = Colore("TestoGrigio");
            etichetta.VerticalAlignment = VerticalAlignment.Center;
            etichetta.Margin = new Thickness(12, 0, 10, 0);

            CheckBox interruttore = new CheckBox();
            interruttore.Style = (Style)Resources["Interruttore"];
            interruttore.IsChecked = !opzioni.Nascosti.Contains(originale);
            interruttore.Tag = originale;
            interruttore.Click += QuandoCambiaSullaBarra;

            Grid.SetColumn(icona, 0);
            Grid.SetColumn(testi, 1);
            Grid.SetColumn(etichetta, 2);
            Grid.SetColumn(interruttore, 3);
            riga.Children.Add(icona);
            riga.Children.Add(testi);
            riga.Children.Add(etichetta);
            riga.Children.Add(interruttore);
            return riga;
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


        // ----- Interruttori e soglia -----

        void QuandoCambiaInterruttore(object sender, RoutedEventArgs e)
        {
            opzioni.MostraPercentuale = MostraPercentuale.IsChecked == true;
            opzioni.Animazioni = Animazioni.IsChecked == true;
            opzioni.AvvisoCompleta = AvvisoCompleta.IsChecked == true;
            opzioni.AvvioWindows = AvvioWindows.IsChecked == true;
            App.Corrente.ImpostazioniCambiate();
        }

        void QuandoCambiaSoglia(object sender, RoutedEventArgs e)
        {
            if (Soglia10.IsChecked == true) opzioni.Soglia = 10;
            if (Soglia20.IsChecked == true) opzioni.Soglia = 20;
            if (Soglia30.IsChecked == true) opzioni.Soglia = 30;
            App.Corrente.SogliaCambiata();
        }
    }
}
