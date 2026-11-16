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


        // ----- Salvataggio -----

        // La cartella dei dati dell'app
        public static string Cartella()
        {
            string cartella = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BatterieInBarra");
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
