// WidgetWindow.xaml.cs
// Il widget: cerchi, lista con il tempo stimato e grafico delle ultime 24 ore.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BatterieInBarra
{
    public partial class WidgetWindow : Window
    {
        List<Vista> ultimaLista = new List<Vista>();
        string dispositivoGrafico = "";   // il dispositivo scelto per il grafico (nome originale)

        // Colori del tema (chiaro o scuro)
        Brush testo;
        Brush testoGrigio;
        Brush linea;
        Brush anelloVuoto;
        Brush bottone;
        Brush bottoneScelto;
        Brush bordoBatteria;

        public WidgetWindow()
        {
            InitializeComponent();
            Deactivated += QuandoPerdeIlFocus;
        }

        // Se clicchi fuori dal widget, si chiude
        void QuandoPerdeIlFocus(object sender, EventArgs e)
        {
            Hide();
            App.Corrente.WidgetChiuso();
        }


        // ----- Tema -----

        void PreparaColori()
        {
            if (Grafica.AppChiare())
            {
                Scheda.Background = Grafica.Pennello("#F8F6F6F8");
                Scheda.BorderBrush = Grafica.Pennello("#1A000000");
                testo = Grafica.Pennello("#1C1C1E");
                testoGrigio = Grafica.Pennello("#6E6E73");
                linea = Grafica.Pennello("#1A000000");
                anelloVuoto = Grafica.Pennello("#1A000000");
                bottone = Grafica.Pennello("#0F000000");
                bottoneScelto = Grafica.Pennello("#29000000");
            }
            else
            {
                Scheda.Background = Grafica.Pennello("#F81C1C1E");
                Scheda.BorderBrush = Grafica.Pennello("#1FFFFFFF");
                testo = Brushes.White;
                testoGrigio = Grafica.Pennello("#98989D");
                linea = Grafica.Pennello("#1FFFFFFF");
                anelloVuoto = Grafica.Pennello("#24FFFFFF");
                bottone = Grafica.Pennello("#2C2C2E");
                bottoneScelto = Grafica.Pennello("#636366");
            }
            bordoBatteria = Grafica.Pennello("#8E8E93");

            Titolo.Foreground = testo;
            TitoloGrafico.Foreground = testo;
            Vuoto.Foreground = testoGrigio;
            Linea1.Background = linea;
            Linea2.Background = linea;
        }


        // ----- Disegno -----

        // La chiama App con la lista dei dispositivi
        public void Mostra(List<Vista> lista)
        {
            ultimaLista = lista;
            PreparaColori();

            Cerchi.Children.Clear();
            Lista.Children.Clear();

            if (lista.Count == 0)
            {
                Vuoto.Visibility = Visibility.Visible;
                ParteGrafico.Visibility = Visibility.Collapsed;
                return;
            }
            Vuoto.Visibility = Visibility.Collapsed;
            ParteGrafico.Visibility = Visibility.Visible;

            foreach (Vista v in lista)
            {
                Cerchi.Children.Add(CreaCerchio(v));
                Lista.Children.Add(CreaRiga(v));
            }

            MostraGrafico();
        }


        // ----- Cerchio con l'anello -----

        FrameworkElement CreaCerchio(Vista v)
        {
            const double LATO = 60;
            const double SPESSORE = 4.5;

            Grid anello = new Grid();
            anello.Width = LATO;
            anello.Height = LATO;

            // L'anello grigio sotto
            Ellipse sotto = new Ellipse();
            sotto.Stroke = anelloVuoto;
            sotto.StrokeThickness = SPESSORE;
            anello.Children.Add(sotto);

            // L'anello colorato: è un cerchio tratteggiato con un solo "tratto" lungo quanto la batteria
            if (v.Batteria > 0)
            {
                double raggio = (LATO - SPESSORE) / 2;
                double circonferenza = 2 * Math.PI * raggio;
                double pieno = circonferenza * v.Batteria / 100.0;

                Ellipse sopra = new Ellipse();
                sopra.Stroke = Grafica.Pennello(Grafica.ColoreBatteria(v.Batteria));
                sopra.StrokeThickness = SPESSORE;
                sopra.StrokeDashCap = PenLineCap.Round;
                // le misure del tratteggio sono in "spessori", non in pixel
                sopra.StrokeDashArray = new DoubleCollection { pieno / SPESSORE, circonferenza / SPESSORE };
                // parte dall'alto (ore 12) invece che da destra (ore 3)
                sopra.RenderTransform = new RotateTransform(-90);
                sopra.RenderTransformOrigin = new Point(0.5, 0.5);
                anello.Children.Add(sopra);
            }

            // L'icona al centro
            FrameworkElement icona = Grafica.CreaIcona(v.Originale, 24, testo);
            icona.HorizontalAlignment = HorizontalAlignment.Center;
            icona.VerticalAlignment = VerticalAlignment.Center;
            anello.Children.Add(icona);

            TextBlock numero = new TextBlock();
            numero.Text = v.Batteria + "%";
            numero.Foreground = testo;
            numero.FontSize = 13;
            numero.FontWeight = FontWeights.SemiBold;
            numero.HorizontalAlignment = HorizontalAlignment.Center;
            numero.Margin = new Thickness(0, 6, 0, 0);

            StackPanel cerchio = new StackPanel();
            cerchio.ToolTip = v.Nome;
            cerchio.Children.Add(anello);
            cerchio.Children.Add(numero);
            return cerchio;
        }


        // ----- Riga della lista -----

        FrameworkElement CreaRiga(Vista v)
        {
            Grid riga = new Grid();
            riga.Margin = new Thickness(0, 7, 0, 7);
            riga.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            riga.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            riga.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Nome e, sotto, il tempo stimato
            StackPanel testi = new StackPanel();

            TextBlock nome = new TextBlock();
            nome.Text = v.Nome;
            nome.Foreground = testo;
            nome.FontSize = 14;
            nome.TextTrimming = TextTrimming.CharacterEllipsis;
            testi.Children.Add(nome);

            TextBlock tempo = new TextBlock();
            tempo.Text = v.Tempo;
            tempo.FontSize = 11;
            if (v.Batteria <= 20)
            {
                tempo.Foreground = Grafica.Pennello(Grafica.Rosso);
            }
            else
            {
                tempo.Foreground = testoGrigio;
            }
            testi.Children.Add(tempo);

            TextBlock numero = new TextBlock();
            numero.Text = v.Batteria + "%";
            numero.Foreground = testoGrigio;
            numero.FontSize = 14;
            numero.VerticalAlignment = VerticalAlignment.Center;
            numero.Margin = new Thickness(10, 0, 10, 0);

            FrameworkElement batteria = Grafica.CreaMiniBatteria(v.Batteria, 30, 14, bordoBatteria);

            Grid.SetColumn(testi, 0);
            Grid.SetColumn(numero, 1);
            Grid.SetColumn(batteria, 2);
            riga.Children.Add(testi);
            riga.Children.Add(numero);
            riga.Children.Add(batteria);
            return riga;
        }


        // ----- Grafico delle ultime 24 ore -----

        void MostraGrafico()
        {
            // Se il dispositivo scelto non c'è più, prendiamo il primo
            Vista scelto = null;
            foreach (Vista v in ultimaLista)
            {
                if (v.Originale == dispositivoGrafico)
                {
                    scelto = v;
                }
            }
            if (scelto == null)
            {
                scelto = ultimaLista[0];
                dispositivoGrafico = scelto.Originale;
            }

            // Bottoni per scegliere il dispositivo
            SceltaGrafico.Children.Clear();
            foreach (Vista v in ultimaLista)
            {
                SceltaGrafico.Children.Add(CreaBottone(v));
            }

            DisegnaGrafico(scelto.Storia, Grafica.ColoreBatteria(scelto.Batteria));
        }

        FrameworkElement CreaBottone(Vista v)
        {
            TextBlock scritta = new TextBlock();
            scritta.Text = v.Nome;
            scritta.FontSize = 12;

            Border bottoneGrafico = new Border();
            bottoneGrafico.CornerRadius = new CornerRadius(12);
            bottoneGrafico.Padding = new Thickness(10, 3, 10, 4);
            bottoneGrafico.Margin = new Thickness(0, 0, 4, 4);
            bottoneGrafico.Cursor = System.Windows.Input.Cursors.Hand;
            bottoneGrafico.Child = scritta;

            if (v.Originale == dispositivoGrafico)
            {
                bottoneGrafico.Background = bottoneScelto;
                scritta.Foreground = testo;
            }
            else
            {
                bottoneGrafico.Background = bottone;
                scritta.Foreground = testoGrigio;
            }

            string originale = v.Originale;
            bottoneGrafico.MouseLeftButtonUp += (s, e) =>
            {
                dispositivoGrafico = originale;
                MostraGrafico();
            };
            return bottoneGrafico;
        }

        void DisegnaGrafico(List<Lettura> punti, Color colore)
        {
            const double L = 280;
            const double A = 80;
            const long VENTIQUATTRO_ORE = 24L * 60 * 60 * 1000;

            Grafico.Children.Clear();
            long adesso = Cronologia.Adesso();
            long inizio = adesso - VENTIQUATTRO_ORE;

            if (punti.Count < 2)
            {
                TextBlock pochi = new TextBlock();
                pochi.Text = "Ancora pochi dati: il grafico si riempie con il tempo.";
                pochi.Foreground = testoGrigio;
                pochi.FontSize = 12;
                pochi.TextWrapping = TextWrapping.Wrap;
                pochi.Width = L;
                Grafico.Children.Add(pochi);
                Grafico.Height = 40;
                return;
            }
            Grafico.Height = A + 18;

            // Righe di sfondo (50% e fondo)
            AggiungiLinea(0, Y(50, A), L, Y(50, A), true);
            AggiungiLinea(0, A, L, A, false);

            // Dividiamo in pezzi: se per più di 20 minuti non ci sono dati, il dispositivo era scollegato
            List<List<Lettura>> pezzi = new List<List<Lettura>>();
            List<Lettura> pezzo = new List<Lettura>();
            pezzo.Add(punti[0]);
            for (int i = 1; i < punti.Count; i++)
            {
                if (punti[i].T - punti[i - 1].T > Cronologia.PAUSA_MASSIMA)
                {
                    pezzi.Add(pezzo);
                    pezzo = new List<Lettura>();
                }
                pezzo.Add(punti[i]);
            }
            pezzi.Add(pezzo);

            // L'area colorata va da colore trasparente (sotto) a colore leggero (sopra)
            LinearGradientBrush sfumatura = new LinearGradientBrush();
            sfumatura.StartPoint = new Point(0, 0);
            sfumatura.EndPoint = new Point(0, 1);
            sfumatura.GradientStops.Add(new GradientStop(Color.FromArgb(90, colore.R, colore.G, colore.B), 0));
            sfumatura.GradientStops.Add(new GradientStop(Color.FromArgb(0, colore.R, colore.G, colore.B), 1));

            foreach (List<Lettura> p in pezzi)
            {
                PointCollection lineaPunti = new PointCollection();
                foreach (Lettura l in p)
                {
                    lineaPunti.Add(new Point(X(l.T, inizio, VENTIQUATTRO_ORE, L), Y(l.P, A)));
                }

                // Se il pezzo arriva fino ad adesso, lo allunghiamo fino al bordo destro
                Lettura ultima = p[p.Count - 1];
                if (adesso - ultima.T < Cronologia.PAUSA_MASSIMA)
                {
                    lineaPunti.Add(new Point(L, Y(ultima.P, A)));
                }

                // Area sotto la linea
                PointCollection areaPunti = new PointCollection(lineaPunti);
                areaPunti.Add(new Point(lineaPunti[lineaPunti.Count - 1].X, A));
                areaPunti.Add(new Point(lineaPunti[0].X, A));

                Polygon area = new Polygon();
                area.Points = areaPunti;
                area.Fill = sfumatura;
                Grafico.Children.Add(area);

                Polyline curva = new Polyline();
                curva.Points = lineaPunti;
                curva.Stroke = Grafica.Pennello(colore);
                curva.StrokeThickness = 2;
                curva.StrokeLineJoin = PenLineJoin.Round;
                Grafico.Children.Add(curva);
            }

            // Scritte sotto
            AggiungiEtichetta("24 ore fa", 0, A + 3, TextAlignment.Left);
            AggiungiEtichetta("12 ore fa", L / 2, A + 3, TextAlignment.Center);
            AggiungiEtichetta("adesso", L, A + 3, TextAlignment.Right);
        }

        // Trasforma ora e percentuale in coordinate
        double X(long t, long inizio, long durata, double larghezza)
        {
            return (double)(t - inizio) / durata * larghezza;
        }

        double Y(int percentuale, double altezza)
        {
            return altezza - percentuale / 100.0 * (altezza - 4);
        }

        void AggiungiLinea(double x1, double y1, double x2, double y2, bool tratteggiata)
        {
            Line riga = new Line();
            riga.X1 = x1;
            riga.Y1 = y1;
            riga.X2 = x2;
            riga.Y2 = y2;
            riga.Stroke = linea;
            riga.StrokeThickness = 1;
            if (tratteggiata)
            {
                riga.StrokeDashArray = new DoubleCollection { 3, 4 };
            }
            Grafico.Children.Add(riga);
        }

        void AggiungiEtichetta(string testoEtichetta, double x, double y, TextAlignment allineamento)
        {
            TextBlock etichetta = new TextBlock();
            etichetta.Text = testoEtichetta;
            etichetta.Foreground = testoGrigio;
            etichetta.FontSize = 10;
            etichetta.Width = 80;
            etichetta.TextAlignment = allineamento;

            // La posizione dipende dall'allineamento (sinistra, centro, destra)
            double sinistra = x;
            if (allineamento == TextAlignment.Center)
            {
                sinistra = x - 40;
            }
            if (allineamento == TextAlignment.Right)
            {
                sinistra = x - 80;
            }
            Canvas.SetLeft(etichetta, sinistra);
            Canvas.SetTop(etichetta, y);
            Grafico.Children.Add(etichetta);
        }
    }
}
