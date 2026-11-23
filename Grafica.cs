// Grafica.cs
// Cose usate da tutte le finestre: colori, tema chiaro/scuro, icone e mini batterie.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

        // ----- I 3 disegni che puoi scegliere per cuffie, controller, mouse e tastiera -----
        // Ogni disegno ha due parti: "linee" (solo il contorno) e "pieno" (parti colorate dentro).
        // "F0" all'inizio vuol dire: i buchi dentro restano vuoti (es. i tasti della tastiera piena)

        // I tipi che si possono personalizzare (stesso ordine delle Impostazioni)
        public static readonly string[] TIPI_PERSONALIZZABILI = { "cuffie", "controller", "mouse", "tastiera" };

        // Le impostazioni (le mette App): servono per sapere che disegno hai scelto
        public static Impostazioni Scelte = null;

        // Restituisce { linee, pieno } del disegno "numero" (1, 2 o 3) di un tipo
        public static string[] Disegno(string tipo, int numero)
        {
            if (tipo == "cuffie")
            {
                if (numero == 1)
                {
                    return new string[] { CUFFIE, "" };
                }
                if (numero == 2)
                {
                    string padiglioni = "M5.25 14 A2.25 2.25 0 0 1 7.5 16.25 V18.75 A2.25 2.25 0 0 1 3 18.75 V16.25 A2.25 2.25 0 0 1 5.25 14 Z " +
                        "M18.75 14 A2.25 2.25 0 0 1 21 16.25 V18.75 A2.25 2.25 0 0 1 16.5 18.75 V16.25 A2.25 2.25 0 0 1 18.75 14 Z";
                    return new string[] { "M3.5 16 V12.5 A8.5 8.5 0 0 1 20.5 12.5 V16 " + padiglioni, padiglioni };
                }
                return new string[] { "M4 15 V12 A8 8 0 0 1 20 12 V15 " +
                    "M4.5 13.5 H5.5 A2 2 0 0 1 7.5 15.5 V19 A2 2 0 0 1 5.5 21 H4.5 A2 2 0 0 1 2.5 19 V15.5 A2 2 0 0 1 4.5 13.5 Z " +
                    "M18.5 13.5 H19.5 A2 2 0 0 1 21.5 15.5 V19 A2 2 0 0 1 19.5 21 H18.5 A2 2 0 0 1 16.5 19 V15.5 A2 2 0 0 1 18.5 13.5 Z", "" };
            }
            if (tipo == "controller")
            {
                if (numero == 1)
                {
                    return new string[] { CONTROLLER, "" };
                }
                if (numero == 2)
                {
                    return new string[] { "", "F0 M6 7.5 H18 A4.5 4.5 0 0 1 22.5 12 L21.7 16.7 A2.5 2.5 0 0 1 17.4 18 L15.2 15.5 H8.8 L6.6 18 A2.5 2.5 0 0 1 2.3 16.7 L1.5 12 A4.5 4.5 0 0 1 6 7.5 Z " +
                        "M6.8 10 H8.2 V11.5 H9.7 V12.9 H8.2 V14.4 H6.8 V12.9 H5.3 V11.5 H6.8 Z " +
                        "M15 10.2 A1 1 0 1 0 17 10.2 A1 1 0 1 0 15 10.2 Z M17 12.4 A1 1 0 1 0 19 12.4 A1 1 0 1 0 17 12.4 Z" };
                }
                return new string[] { "M7 7 H17 C20 7 22 10 22 14 C22 17 20 18.5 18 17 L16 15.5 H8 L6 17 C4 18.5 2 17 2 14 C2 10 4 7 7 7 Z " +
                    "M6.2 11.5 A1.6 1.6 0 1 0 9.4 11.5 A1.6 1.6 0 1 0 6.2 11.5 Z M12.7 13 A1.3 1.3 0 1 0 15.3 13 A1.3 1.3 0 1 0 12.7 13 Z " +
                    "M16.8 10 H16.81 M18.6 11.6 H18.61", "" };
            }
            if (tipo == "mouse")
            {
                if (numero == 1)
                {
                    return new string[] { MOUSE, "" };
                }
                if (numero == 2)
                {
                    return new string[] { "", "F0 M12 2.5 A6.5 6.5 0 0 1 18.5 9 V15 A6.5 6.5 0 0 1 5.5 15 V9 A6.5 6.5 0 0 1 12 2.5 Z M11.1 6 H12.9 V11 H11.1 Z" };
                }
                return new string[] { "M12 3 A6 6 0 0 1 18 9 V15 A6 6 0 0 1 6 15 V9 A6 6 0 0 1 12 3 Z M6 10 H18 M12 3 V10", "" };
            }
            if (tipo == "tastiera")
            {
                if (numero == 1)
                {
                    return new string[] { TASTIERA, "" };
                }
                if (numero == 2)
                {
                    return new string[] { "", "F0 M4.5 5.5 H19.5 A3 3 0 0 1 22.5 8.5 V15.5 A3 3 0 0 1 19.5 18.5 H4.5 A3 3 0 0 1 1.5 15.5 V8.5 A3 3 0 0 1 4.5 5.5 Z " +
                        "M5 9 H7 V11 H5 Z M9 9 H11 V11 H9 Z M13 9 H15 V11 H13 Z M17 9 H19 V11 H17 Z M7 13.5 H17 V15.3 H7 Z" };
                }
                return new string[] { "M4.5 6 H19.5 A2.5 2.5 0 0 1 22 8.5 V15.5 A2.5 2.5 0 0 1 19.5 18 H4.5 A2.5 2.5 0 0 1 2 15.5 V8.5 A2.5 2.5 0 0 1 4.5 6 Z " +
                    "M5.5 9.5 H6.5 M8.5 9.5 H9.5 M11.5 9.5 H12.5 M14.5 9.5 H15.5 M17.5 9.5 H18.5 " +
                    "M5.5 12 H6.5 M8.5 12 H9.5 M11.5 12 H12.5 M14.5 12 H15.5 M17.5 12 H18.5 M7.5 14.8 H16.5", "" };
            }

            // Gli altri tipi hanno un solo disegno
            switch (tipo)
            {
                case "auricolari": return new string[] { AURICOLARI, "" };
                case "portatile": return new string[] { PORTATILE, "" };
            }
            return new string[] { BLUETOOTH, "" };
        }

        // Crea l'icona del dispositivo, grande "lato" pixel (usa il disegno scelto nelle Impostazioni)
        public static FrameworkElement CreaIcona(string nome, double lato, Brush colore)
        {
            string tipo = TipoDispositivo(nome);
            if (Scelte == null)
            {
                Scelte = new Impostazioni();   // non ancora pronte: usiamo i disegni di base
            }

            // Hai caricato una tua immagine per questo tipo?
            string personale = Scelte.IconaPersonaleDi(tipo);
            if (personale != null)
            {
                FrameworkElement immagine = CreaImmagine(personale, lato);
                if (immagine != null)
                {
                    return immagine;
                }
            }
            return CreaIconaTipo(tipo, Scelte.ModelloDi(tipo), lato, colore);
        }

        // Crea il disegno "numero" di un tipo (lo usano anche le Impostazioni per far vedere i 3 disegni)
        public static FrameworkElement CreaIconaTipo(string tipo, int numero, double lato, Brush colore)
        {
            string[] disegno = Disegno(tipo, numero);

            // Il disegno è 24x24: lo mettiamo in una tela 24x24 e lo ingrandiamo alla misura giusta
            Canvas tela = new Canvas();
            tela.Width = 24;
            tela.Height = 24;

            // Parte piena
            if (disegno[1] != "")
            {
                System.Windows.Shapes.Path pieno = new System.Windows.Shapes.Path();
                pieno.Data = Geometry.Parse(disegno[1]);
                pieno.Fill = colore;
                tela.Children.Add(pieno);
            }

            // Contorno
            if (disegno[0] != "")
            {
                System.Windows.Shapes.Path linee = new System.Windows.Shapes.Path();
                linee.Data = Geometry.Parse(disegno[0]);
                linee.Stroke = colore;
                linee.StrokeThickness = 1.8;
                linee.StrokeLineJoin = PenLineJoin.Round;
                linee.StrokeStartLineCap = PenLineCap.Round;
                linee.StrokeEndLineCap = PenLineCap.Round;
                tela.Children.Add(linee);
            }

            Viewbox scatola = new Viewbox();
            scatola.Width = lato;
            scatola.Height = lato;
            scatola.Child = tela;
            return scatola;
        }

        // Un'immagine PNG caricata da te (null se non si riesce ad aprirla)
        public static FrameworkElement CreaImmagine(string file, double lato)
        {
            try
            {
                BitmapImage foto = new BitmapImage();
                foto.BeginInit();
                foto.CacheOption = BitmapCacheOption.OnLoad;               // legge subito il file e non lo tiene aperto
                foto.CreateOptions = BitmapCreateOptions.IgnoreImageCache;  // se la cambi, si vede quella nuova
                foto.UriSource = new Uri(file);
                foto.EndInit();

                Image immagine = new Image();
                immagine.Source = foto;
                immagine.Width = lato;
                immagine.Height = lato;
                immagine.Stretch = Stretch.Uniform;
                RenderOptions.SetBitmapScalingMode(immagine, BitmapScalingMode.HighQuality);
                return immagine;
            }
            catch (Exception)
            {
                return null;
            }
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
