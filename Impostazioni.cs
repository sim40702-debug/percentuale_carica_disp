// Impostazioni.cs
// Legge e salva le impostazioni in un file JSON.
// Il file sta in %APPDATA%\BatterieInBarra (non nel progetto).

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace BatterieInBarra
{
    public class Impostazioni
    {
        // Nome originale -> nome scelto da te (es. "WH-1000XM5" -> "Cuffie")
        public Dictionary<string, string> Nomi { get; set; } = new Dictionary<string, string>();

        // Dispositivi da non mostrare sulla barra
        public List<string> Nascosti { get; set; } = new List<string>();

        // Tutti i dispositivi visti almeno una volta, nell'ordine scelto da te (da sinistra a destra)
        public List<string> Visti { get; set; } = new List<string>();

        public bool MostraPercentuale { get; set; } = true;   // percentuale sulla barra
        public bool Animazioni { get; set; } = true;          // dissolvenza quando si collega o si scollega
        public int Soglia { get; set; } = 20;                 // avviso batteria scarica (10, 20 o 30)
        public bool AvvisoCompleta { get; set; } = true;      // avviso quando torna al 100%
        public bool AvvioWindows { get; set; } = true;        // parte da sola all'accensione
        public int Spostamento { get; set; } = 0;             // pixel spostati a mano con il menu
        public bool ControllaAggiornamenti { get; set; } = true;  // cerca da sola le versioni nuove

        // Colori
        public bool TuttoBianco { get; set; } = false;   // batterie bianche come su Apple (rosse solo se scariche)
        public int SogliaGiallo { get; set; } = 40;      // gialla sotto questa %
        public int SogliaRosso { get; set; } = 20;       // rossa sotto questa %

        // Promemoria serale
        public bool PromemoriaSerale { get; set; } = true;
        public int OraPromemoria { get; set; } = 22;
        public string UltimoPromemoria { get; set; } = "";   // giorno dell'ultimo promemoria (es. "2026-11-16")

        // Batteria del PC nel widget (solo sui portatili)
        public bool MostraBatteriaPC { get; set; } = true;

        // Non disturbare: niente avvisi in questi orari e durante i giochi a schermo intero (arrivano dopo)
        public bool NonDisturbare { get; set; } = true;
        public int SilenzioDalle { get; set; } = 23;
        public int SilenzioAlle { get; set; } = 7;

        // Scorciatoia da tastiera Ctrl + Alt + B per aprire il widget
        public bool Scorciatoia { get; set; } = true;

        // Lingua dell'app: "auto" (quella di Windows), "it", "en" o "de"
        public string Lingua { get; set; } = "auto";

        // Icone: tipo ("cuffie", "controller", "mouse", "tastiera") -> disegno scelto (1, 2 o 3)
        public Dictionary<string, int> ModelliIcone { get; set; } = new Dictionary<string, int>();

        // Icone caricate da te: tipo -> file PNG (copiato nella cartella dei dati, sottocartella "icone")
        public Dictionary<string, string> IconePersonali { get; set; } = new Dictionary<string, string>();

        // Il disegno scelto per un tipo. Se non hai scelto: cuffie piene (2), gli altri a linee (1)
        public int ModelloDi(string tipo)
        {
            if (ModelliIcone != null && ModelliIcone.ContainsKey(tipo))
            {
                int numero = ModelliIcone[tipo];
                if (numero >= 1 && numero <= 3)
                {
                    return numero;
                }
            }
            if (tipo == "cuffie")
            {
                return 2;
            }
            return 1;
        }

        // L'immagine caricata da te per un tipo (null se non c'è o il file è sparito)
        public string IconaPersonaleDi(string tipo)
        {
            if (IconePersonali != null && IconePersonali.ContainsKey(tipo) && File.Exists(IconePersonali[tipo]))
            {
                return IconePersonali[tipo];
            }
            return null;
        }


        // ----- Salvataggio -----

        // Nei test usiamo un'altra cartella, così i dati veri non vengono toccati
        public static string CartellaDiProva = null;

        // La cartella dei dati dell'app
        public static string Cartella()
        {
            string cartella = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BatterieInBarra");
            if (CartellaDiProva != null)
            {
                cartella = CartellaDiProva;
            }
            Directory.CreateDirectory(cartella);
            return cartella;
        }

        static string PercorsoFile()
        {
            return Path.Combine(Cartella(), "impostazioni.json");
        }

        public static Impostazioni Leggi()
        {
            try
            {
                string testo = System.IO.File.ReadAllText(PercorsoFile());
                Impostazioni lette = JsonSerializer.Deserialize<Impostazioni>(testo);
                if (lette != null)
                {
                    return lette;
                }
            }
            catch (Exception)
            {
                // il file non c'è ancora: usiamo i valori di partenza
            }
            return new Impostazioni();
        }

        public void Salva()
        {
            JsonSerializerOptions opzioni = new JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(PercorsoFile(), JsonSerializer.Serialize(this, opzioni));
        }


        // ----- Comodità -----

        // A quest'ora siamo nel silenzio di "Non disturbare"? (es. dalle 23 alle 7)
        public static bool OraSilenziosa(int ora, int dalle, int alle)
        {
            if (dalle > alle)
            {
                // passa la mezzanotte (es. dalle 23 alle 7)
                return ora >= dalle || ora < alle;
            }
            return ora >= dalle && ora < alle;
        }

        // Il nome da mostrare (quello scelto da te, oppure l'originale)
        public string NomeDi(string originale)
        {
            string nome;
            if (Nomi.TryGetValue(originale, out nome))
            {
                return nome;
            }
            return originale;
        }

        // Avvio con Windows: scriviamo (o togliamo) l'app nella lista "Esegui" di Windows
        public void ApplicaAvvioWindows()
        {
            using (RegistryKey chiave = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (chiave == null)
                {
                    return;
                }
                if (AvvioWindows)
                {
                    chiave.SetValue("BatterieInBarra", "\"" + Environment.ProcessPath + "\"");
                }
                else
                {
                    chiave.DeleteValue("BatterieInBarra", false);
                }
            }
        }
    }
}
