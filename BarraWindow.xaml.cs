// BarraWindow.xaml.cs
// Disegna le mini batterie sulla barra di Windows, con la dissolvenza
// quando un dispositivo si collega o si scollega.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BatterieInBarra
{
    public partial class BarraWindow : Window
    {
        const int DURATA_DISSOLVENZA = 350;

        // Gli elementi già sulla barra: nome originale -> elemento
        Dictionary<string, Border> elementi = new Dictionary<string, Border>();
        bool mostraPercentuale = true;
        ContextMenu menu;

        public BarraWindow()
        {
            InitializeComponent();
            SourceInitialized += QuandoCreata;
            CreaMenu();
        }

        // Appena Windows crea la finestra: non deve "rubare" il clic
        void QuandoCreata(object sender, EventArgs e)
        {
            Finestre.RendiNonAttivabile(this);
        }


        // ----- Colori (barra di Windows chiara o scura) -----

        Brush ColoreTesto()
        {
            return Grafica.BarraChiara() ? Grafica.Pennello("#1C1C1E") : Brushes.White;
        }

        Brush ColoreBordo()
        {
            return Grafica.BarraChiara() ? Grafica.Pennello("#99000000") : Grafica.Pennello("#BFFFFFFF");
        }

        Brush ColoreSopra()
        {
            return Grafica.BarraChiara() ? Grafica.Pennello("#0F000000") : Grafica.Pennello("#14FFFFFF");
        }


        // ----- Disegno -----

        // La chiama App con i dispositivi da mostrare
        public void Mostra(List<Vista> lista, bool percentuale, bool animazioni)
        {
            mostraPercentuale = percentuale;

            // Nessun dispositivo: mostriamo solo l'icona Bluetooth chiara
            if (lista.Count == 0)
            {
                Vista nessuno = new Vista();
                nessuno.Originale = "nessuno";
                nessuno.Nome = "Nessun dispositivo collegato";
                nessuno.Batteria = -1;
                lista = new List<Vista>();
                lista.Add(nessuno);
            }

            // 1. Togliamo (con dissolvenza) quelli che non ci sono più
            List<string> nomiNuovi = new List<string>();
            foreach (Vista v in lista)
            {
                nomiNuovi.Add(v.Originale);
            }

            List<string> daTogliere = new List<string>();
            foreach (string nome in elementi.Keys)
            {
                if (!nomiNuovi.Contains(nome))
                {
                    daTogliere.Add(nome);
                }
            }
            foreach (string nome in daTogliere)
            {
                TogliElemento(elementi[nome], animazioni);
                elementi.Remove(nome);
            }

            // 2. Aggiorniamo quelli che ci sono e aggiungiamo quelli nuovi
            foreach (Vista v in lista)
            {
                Border elemento;
                if (!elementi.TryGetValue(v.Originale, out elemento))
                {
                    elemento = CreaElemento();
                    elementi[v.Originale] = elemento;
                    Contenitore.Children.Add(elemento);
                    if (animazioni)
                    {
                        AnimaEntrata(elemento);
                    }
                }
                RiempiElemento(elemento, v);
            }
        }

        // Il riquadro di un dispositivo (vuoto: lo riempie RiempiElemento)
        Border CreaElemento()
        {
            Border elemento = new Border();
            elemento.Height = 40;
            elemento.Margin = new Thickness(1, 0, 1, 0);
            elemento.CornerRadius = new CornerRadius(6);
            elemento.Background = Grafica.Pennello("#01FFFFFF");   // quasi trasparente, ma cliccabile

            elemento.MouseEnter += QuandoMouseSopra;
            elemento.MouseLeave += QuandoMouseFuori;
            elemento.MouseLeftButtonUp += QuandoClicSinistro;
            elemento.MouseRightButtonUp += QuandoClicDestro;
            return elemento;
        }

        // Scrive dentro il riquadro: icona, percentuale e mini batteria
        void RiempiElemento(Border elemento, Vista v)
        {
            Brush testo = ColoreTesto();

            StackPanel riga = new StackPanel();
            riga.Orientation = Orientation.Horizontal;
            riga.HorizontalAlignment = HorizontalAlignment.Center;
            riga.VerticalAlignment = VerticalAlignment.Center;

            // Caso speciale: nessun dispositivo
            if (v.Batteria < 0)
            {
                elemento.Width = 40;
                riga.Opacity = 0.5;
                elemento.ToolTip = v.Nome;
                riga.Children.Add(Grafica.CreaIcona("bluetooth", 16, testo));
                elemento.Child = riga;
                return;
            }

            elemento.ToolTip = v.Nome + ": " + v.Batteria + "%";
            elemento.Width = mostraPercentuale ? 88 : 62;

            // [0] Icona (dal nome originale, così funziona anche se lo rinomini)
            FrameworkElement icona = Grafica.CreaIcona(v.Originale, 16, testo);
            icona.Margin = new Thickness(0, 0, 5, 0);
            riga.Children.Add(icona);

            // [1] Percentuale
            TextBlock numero = new TextBlock();
            numero.Text = v.Batteria + "%";
            numero.Foreground = testo;
            numero.FontSize = 12;
            numero.MinWidth = 28;
            numero.TextAlignment = TextAlignment.Right;
            numero.VerticalAlignment = VerticalAlignment.Center;
            numero.Margin = new Thickness(0, 0, 5, 0);
            if (!mostraPercentuale)
            {
                numero.Visibility = Visibility.Collapsed;
            }
            riga.Children.Add(numero);

            // [2] Mini batteria
            riga.Children.Add(Grafica.CreaMiniBatteria(v.Batteria, 23, 12, ColoreBordo()));

            elemento.Child = riga;
        }


        // ----- Mouse -----

        void QuandoMouseSopra(object sender, MouseEventArgs e)
        {
            Border elemento = (Border)sender;
            elemento.Background = ColoreSopra();

            // Solo batteria: la percentuale compare al posto dell'icona
            StackPanel riga = elemento.Child as StackPanel;
            if (!mostraPercentuale && riga != null && riga.Children.Count == 3)
            {
                riga.Children[0].Visibility = Visibility.Collapsed;
                riga.Children[1].Visibility = Visibility.Visible;
            }
        }

        void QuandoMouseFuori(object sender, MouseEventArgs e)
        {
            Border elemento = (Border)sender;
            elemento.Background = Grafica.Pennello("#01FFFFFF");

            StackPanel riga = elemento.Child as StackPanel;
            if (!mostraPercentuale && riga != null && riga.Children.Count == 3)
            {
                riga.Children[0].Visibility = Visibility.Visible;
                riga.Children[1].Visibility = Visibility.Collapsed;
            }
        }

        // Clic sinistro: apre o chiude il widget
        void QuandoClicSinistro(object sender, MouseButtonEventArgs e)
        {
            App.Corrente.ApriChiudiWidget();
        }

        // Clic destro: menu
        void QuandoClicDestro(object sender, MouseButtonEventArgs e)
        {
            menu.PlacementTarget = this;
            menu.Placement = PlacementMode.MousePoint;
            menu.IsOpen = true;
            e.Handled = true;
        }


        // ----- Menu del tasto destro -----

        void CreaMenu()
        {
            menu = new ContextMenu();
            menu.Items.Add(CreaVoce("Aggiorna ora", QuandoAggiorna));
            menu.Items.Add(CreaVoce("Impostazioni…", QuandoImpostazioni));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreaVoce("Sposta a sinistra", QuandoSinistra));
            menu.Items.Add(CreaVoce("Sposta a destra", QuandoDestra));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreaVoce("Esci", QuandoEsci));
        }

        MenuItem CreaVoce(string testo, RoutedEventHandler quandoClic)
        {
            MenuItem voce = new MenuItem();
            voce.Header = testo;
            voce.Click += quandoClic;
            return voce;
        }

        async void QuandoAggiorna(object sender, RoutedEventArgs e)
        {
            await App.Corrente.Aggiorna();
        }

        void QuandoImpostazioni(object sender, RoutedEventArgs e)
        {
            App.Corrente.ApriImpostazioni();
        }

        void QuandoSinistra(object sender, RoutedEventArgs e)
        {
            App.Corrente.CambiaSpostamento(20);
        }

        void QuandoDestra(object sender, RoutedEventArgs e)
        {
            App.Corrente.CambiaSpostamento(-20);
        }

        void QuandoEsci(object sender, RoutedEventArgs e)
        {
            App.Corrente.Esci();
        }


        // ----- Dissolvenza -----

        void AnimaEntrata(Border elemento)
        {
            TimeSpan durata = TimeSpan.FromMilliseconds(DURATA_DISSOLVENZA);

            ScaleTransform grandezza = new ScaleTransform(0.8, 0.8);
            elemento.RenderTransform = grandezza;
            elemento.RenderTransformOrigin = new Point(0.5, 0.5);

            elemento.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, durata));
            grandezza.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.8, 1, durata));
            grandezza.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.8, 1, durata));
        }

        void TogliElemento(Border elemento, bool animazioni)
        {
            if (!animazioni)
            {
                Contenitore.Children.Remove(elemento);
                return;
            }

            TimeSpan durata = TimeSpan.FromMilliseconds(DURATA_DISSOLVENZA);

            ScaleTransform grandezza = new ScaleTransform(1, 1);
            elemento.RenderTransform = grandezza;
            elemento.RenderTransformOrigin = new Point(0.5, 0.5);

            DoubleAnimation sparisce = new DoubleAnimation(1, 0, durata);
            sparisce.Completed += (s, e) => Contenitore.Children.Remove(elemento);   // finita la dissolvenza, lo togliamo
            elemento.BeginAnimation(OpacityProperty, sparisce);
            grandezza.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, 0.8, durata));
            grandezza.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, 0.8, durata));
        }
    }
}
