// Registro.cs
// Un piccolo "diario" dell'app (registro.txt in %APPDATA%\BatterieInBarra):
// scrive quando parte, gli aggiornamenti e gli errori. Serve per capire i problemi a distanza.

using System;
using System.Collections.Generic;
using System.IO;

namespace BatterieInBarra
{
    public static class Registro
    {
        const long GRANDEZZA_MASSIMA = 200 * 1024;   // oltre 200 KB ricomincia da capo
        static readonly object lucchetto = new object();

        static string PercorsoFile()
        {
            return Path.Combine(Impostazioni.Cartella(), "registro.txt");
        }

        public static void Scrivi(string testo)
        {
            try
            {
                lock (lucchetto)
                {
                    string file = PercorsoFile();
                    if (File.Exists(file) && new FileInfo(file).Length > GRANDEZZA_MASSIMA)
                    {
                        File.Delete(file);
                    }
                    File.AppendAllText(file, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + testo + Environment.NewLine);
                }
            }
            catch (Exception)
            {
                // se non si può scrivere il registro, pazienza
            }
        }

        // Le ultime righe (per le informazioni del supporto)
        public static string Ultime(int quante)
        {
            try
            {
                string[] righe = File.ReadAllLines(PercorsoFile());
                List<string> ultime = new List<string>();
                for (int i = Math.Max(0, righe.Length - quante); i < righe.Length; i++)
                {
                    ultime.Add(righe[i]);
                }
                return string.Join(Environment.NewLine, ultime);
            }
            catch (Exception)
            {
                return "(registro vuoto)";
            }
        }
    }
}
