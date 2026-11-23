// Grafica.cs
// Cose usate da tutte le finestre: colori, tema chiaro/scuro, icone e mini batterie.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace BatterieInBarra
{
    public static class Grafica
    {
        // ----- Colori -----

        public static Color Verde = Color.FromRgb(0x34, 0xC7, 0x59);
        public static Color Giallo = Color.FromRgb(0xFF, 0xCC, 0x00);
        public static Color Rosso = Color.FromRgb(0xFF, 0x3B, 0x30);

        // Le scelte dei colori (le aggiorna App dalle impostazioni)
        public static int SogliaGiallo = 40;
        public static int SogliaRosso = 20;
        public static bool TuttoBianco = false;
        // Il colore "bianco" cambia: bianco sulla barra scura, grigio scuro su quella chiara
        public static Color ColoreNeutro = Colors.White;

        // Sceglie il colore in base alla percentuale
        public static Color ColoreBatteria(int percentuale)
        {
            if (percentuale <= SogliaRosso)
            {
                return Rosso;
            }
            if (TuttoBianco)
            {
                return ColoreNeutro;
            }
            if (percentuale <= SogliaGiallo)
            {
                return Giallo;
            }
            return Verde;
        }

        // Crea un "pennello" (colore per WPF) da un codice tipo "#1C1C1E"
        public static SolidColorBrush Pennello(string codice)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(codice));
        }

        public static SolidColorBrush Pennello(Color colore)
        {
            return new SolidColorBrush(colore);
        }


        // ----- Tema chiaro o scuro (come Windows) -----

        // La barra di Windows è chiara?
        public static bool BarraChiara()
        {
            return LeggiTema("SystemUsesLightTheme");
        }

        // Le app sono chiare?
        public static bool AppChiare()
        {
            return LeggiTema("AppsUseLightTheme");
        }

        static bool LeggiTema(string nome)
        {
            try
            {
                using (RegistryKey chiave = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    object valore = chiave?.GetValue(nome);
                    return valore is int && (int)valore == 1;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }


        // ----- Icone dei dispositivi (disegni a linee, 24x24) -----

        // Cuffie con i padiglioni morbidi (la proposta B scelta da Simone)
        const string CUFFIE = "M5 13 V11.5 A7 7 0 0 1 19 11.5 V13 " +
            "M5 13 H6.5 A1.5 1.5 0 0 1 8 14.5 V19.5 A1.5 1.5 0 0 1 6.5 21 H5.5 A2.5 2.5 0 0 1 3 18.5 V15 A2 2 0 0 1 5 13 Z " +
            "M19 13 H17.5 A1.5 1.5 0 0 0 16 14.5 V19.5 A1.5 1.5 0 0 0 17.5 21 H18.5 A2.5 2.5 0 0 0 21 18.5 V15 A2 2 0 0 0 19 13 Z";

        // Due auricolari (AirPods, Galaxy Buds...): la "testa" tonda e il gambo
        const string AURICOLARI = "M7 3 A3.5 3.5 0 1 1 7 10 A3.5 3.5 0 0 1 7 3 Z M9.5 9 V20 " +
            "M17 3 A3.5 3.5 0 1 0 17 10 A3.5 3.5 0 0 0 17 3 Z M14.5 9 V20";

        const string TASTIERA = "M4.5 6 H19.5 A2.5 2.5 0 0 1 22 8.5 V15.5 A2.5 2.5 0 0 1 19.5 18 H4.5 A2.5 2.5 0 0 1 2 15.5 V8.5 A2.5 2.5 0 0 1 4.5 6 Z " +
            "M6 10 H6.01 M10 10 H10.01 M14 10 H14.01 M18 10 H18.01 M7 14 H17";

        const string MOUSE = "M12 3 A6 6 0 0 1 18 9 V15 A6 6 0 0 1 6 15 V9 A6 6 0 0 1 12 3 Z M12 7 V11";

        const string CONTROLLER = "M6 8 H18 A4 4 0 0 1 22 12 L21.2 16.5 A2 2 0 0 1 17.8 17.6 L15.5 15 H8.5 L6.2 17.6 A2 2 0 0 1 2.8 16.5 L2 12 A4 4 0 0 1 6 8 Z " +
            "M7.5 10.5 V13.5 M6 12 H9 M16 11 H16.01 M18 13 H18.01";

        const string BLUETOOTH = "M7 7 L17 17 L12 22 V2 L17 7 L7 17";

        const string PORTATILE = "M5 5 H19 A1 1 0 0 1 20 6 V15 H4 V6 A1 1 0 0 1 5 5 Z M2 18 H22";

        // Il nome speciale usato per la batteria del PC
        public const string NOME_PC = "__pc__";

        // Indovina il tipo di dispositivo dal nome
        public static string TipoDispositivo(string nome)
        {
            if (nome == NOME_PC)
            {
                return "portatile";
            }
            string n = nome.ToLower();

            string[] auricolari = { "airpod", "buds", "auricolari", "wf-", "pods" };
            string[] cuffie = { "cuffie", "head", "wh-", "jbl", "bose", "beats" };
            string[] tastiera = { "keyboard", "tastiera", "keys", "k380", "k780" };
            string[] mouse = { "mouse", "mx master", "mx anywhere" };
            string[] controller = { "controller", "gamepad", "xbox", "dualsense", "dualshock", "joy-con" };

            if (Contiene(n, auricolari)) return "auricolari";
            if (Contiene(n, cuffie)) return "cuffie";
            if (Contiene(n, tastiera)) return "tastiera";
            if (Contiene(n, mouse)) return "mouse";
            if (Contiene(n, controller)) return "controller";
            return "bluetooth";
        }

        static bool Contiene(string testo, string[] parole)
        {
            foreach (string parola in parole)
            {
                if (testo.Contains(parola))
                {
                    return true;
                }
            }
            return false;
        }

        // Crea l'icona del dispositivo, grande "lato" pixel
        public static FrameworkElement CreaIcona(string nome, double lato, Brush colore)
        {
            string disegno = BLUETOOTH;
            switch (TipoDispositivo(nome))
            {
                case "auricolari": disegno = AURICOLARI; break;
                case "cuffie": disegno = CUFFIE; break;
                case "tastiera": disegno = TASTIERA; break;
                case "mouse": disegno = MOUSE; break;
                case "controller": disegno = CONTROLLER; break;
                case "portatile": disegno = PORTATILE; break;
            }

            System.Windows.Shapes.Path linee = new System.Windows.Shapes.Path();
            linee.Data = Geometry.Parse(disegno);
            linee.Stroke = colore;
            linee.StrokeThickness = 1.8;
            linee.StrokeLineJoin = PenLineJoin.Round;
            linee.StrokeStartLineCap = PenLineCap.Round;
            linee.StrokeEndLineCap = PenLineCap.Round;

            // Il disegno è 24x24: lo mettiamo in una tela 24x24 e lo ingrandiamo alla misura giusta
            Canvas tela = new Canvas();
            tela.Width = 24;
            tela.Height = 24;
            tela.Children.Add(linee);

            Viewbox scatola = new Viewbox();
            scatola.Width = lato;
            scatola.Height = lato;
            scatola.Child = tela;
            return scatola;
        }


        // ----- Mini batteria -----

        // Crea una mini batteria che si riempie con la percentuale.
        // "inCarica": riempimento verde e un fulmine giallo in mezzo
        public static FrameworkElement CreaMiniBatteria(int percentuale, double larghezza, double altezza, Brush bordo, bool inCarica = false)
        {
            double spessore = Math.Max(1.2, altezza * 0.11);
            double margine = spessore;

            // Il corpo della batteria
            Border corpo = new Border();
            corpo.Width = larghezza;
            corpo.Height = altezza;
            corpo.BorderBrush = bordo;
            corpo.BorderThickness = new Thickness(spessore);
            corpo.CornerRadius = new CornerRadius(altezza * 0.3);
            corpo.Padding = new Thickness(margine);

            // Il riempimento colorato
            double spazioDentro = larghezza - (spessore + margine) * 2;
            Border carica = new Border();
            carica.HorizontalAlignment = HorizontalAlignment.Left;
            carica.Width = Math.Max(0, spazioDentro * percentuale / 100.0);
            carica.Background = Pennello(ColoreBatteria(percentuale));
            if (inCarica)
            {
                carica.Background = Pennello(ColoreBatteria(100));   // in carica è sempre verde
            }
            carica.CornerRadius = new CornerRadius(altezza * 0.12);
            corpo.Child = carica;

            // La punta a destra
            Border punta = new Border();
            punta.Width = Math.Max(1.5, altezza * 0.18);
            punta.Height = altezza * 0.36;
            punta.Background = bordo;
            punta.CornerRadius = new CornerRadius(0, 1.5, 1.5, 0);
            punta.Margin = new Thickness(1, 0, 0, 0);
            punta.VerticalAlignment = VerticalAlignment.Center;

            // Corpo e fulmine uno sopra l'altro (il fulmine sta al centro del corpo)
            Grid corpoConFulmine = new Grid();
            corpoConFulmine.Children.Add(corpo);
            if (inCarica)
            {
                corpoConFulmine.Children.Add(CreaFulmine(altezza));
            }

            StackPanel batteria = new StackPanel();
            batteria.Orientation = Orientation.Horizontal;
            batteria.VerticalAlignment = VerticalAlignment.Center;
            batteria.Children.Add(corpoConFulmine);
            batteria.Children.Add(punta);
            return batteria;
        }

        // Il fulmine giallo con un bordino scuro, così si vede sia sul verde sia sul vuoto
        static FrameworkElement CreaFulmine(double altezza)
        {
            System.Windows.Shapes.Path fulmine = new System.Windows.Shapes.Path();
            fulmine.Data = Geometry.Parse("M 6,0 L 0,8 L 4.5,8 L 3,14 L 10,5 L 5.5,5 L 7,0 Z");
            fulmine.Fill = Pennello("#FFD60A");
            fulmine.Stroke = Pennello("#B0000000");
            fulmine.StrokeThickness = 1;
            fulmine.Stretch = Stretch.Uniform;
            fulmine.Height = altezza * 0.95;
            fulmine.HorizontalAlignment = HorizontalAlignment.Center;
            fulmine.VerticalAlignment = VerticalAlignment.Center;
            fulmine.IsHitTestVisible = false;
            return fulmine;
        }
    }
}
