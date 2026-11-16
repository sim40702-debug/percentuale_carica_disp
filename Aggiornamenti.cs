// Aggiornamenti.cs
// Controlla su GitHub se c'è una versione nuova, la scarica e sostituisce quella vecchia.
// I dati (impostazioni e cronologia) stanno in %APPDATA%\BatterieInBarra: l'aggiornamento non li tocca.

using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace BatterieInBarra
{
    // Una versione nuova trovata su GitHub
    public class NuovaVersione
    {
        public string Versione;    // es. "0.2.1"
        public string Indirizzo;   // da dove scaricare l'exe
    }

    public static class Aggiornamenti
    {
        // Il repository GitHub dove ci sono le release (deve essere pubblico)
        const string REPOSITORY = "sim40702-debug/percentuale_carica_disp";

        // La versione di questa app (es. "0.2.0")
        public static string VersioneAttuale()
        {
            Version v = Assembly.GetEntryAssembly().GetName().Version;
            return v.Major + "." + v.Minor + "." + v.Build;
        }

        // Chiede a GitHub l'ultima release. Restituisce null se non c'è niente di nuovo.
        public static async Task<NuovaVersione> Controlla()
        {
            using (HttpClient internet = CreaClient())
            {
                string testo = await internet.GetStringAsync("https://api.github.com/repos/" + REPOSITORY + "/releases/latest");

                using (JsonDocument risposta = JsonDocument.Parse(testo))
                {
                    string tag = risposta.RootElement.GetProperty("tag_name").GetString();
                    string versione = tag.TrimStart('v');

                    if (!EPiuNuova(versione, VersioneAttuale()))
                    {
                        return null;
                    }

                    // Cerchiamo il file .exe nella release
                    foreach (JsonElement file in risposta.RootElement.GetProperty("assets").EnumerateArray())
                    {
                        string nome = file.GetProperty("name").GetString();
                        if (nome.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            NuovaVersione nuova = new NuovaVersione();
                            nuova.Versione = versione;
                            nuova.Indirizzo = file.GetProperty("browser_download_url").GetString();
                            return nuova;
                        }
                    }
                }
            }
            return null;
        }

        // "0.2.1" è più nuova di "0.2.0"?
        static bool EPiuNuova(string a, string b)
        {
            Version va;
            Version vb;
            if (!Version.TryParse(a, out va) || !Version.TryParse(b, out vb))
            {
                return false;
            }
            return va > vb;
        }

        // Scarica la versione nuova e la mette al posto di quella vecchia.
        // Windows non permette di cancellare un programma acceso, ma permette di rinominarlo:
        // l'exe acceso diventa "BatterieInBarra.vecchio.exe" e lo cancelliamo alla prossima partenza.
        // La versione nuova si chiama sempre "BatterieInBarra.exe", nella stessa cartella.
        // Restituisce il percorso della versione nuova (da avviare).
        public static async Task<string> Installa(NuovaVersione nuova)
        {
            string attuale = Environment.ProcessPath;
            string cartella = Path.GetDirectoryName(attuale);
            string scaricato = Path.Combine(cartella, "BatterieInBarra.nuovo.exe");
            string vecchio = Path.Combine(cartella, "BatterieInBarra.vecchio.exe");
            string percorsoNuovo = Path.Combine(cartella, "BatterieInBarra.exe");

            // 1. Scarica
            using (HttpClient internet = CreaClient())
            {
                byte[] dati = await internet.GetByteArrayAsync(nuova.Indirizzo);
                await File.WriteAllBytesAsync(scaricato, dati);
            }

            // 2. Mette da parte quello vecchio (acceso) e al suo posto quello nuovo
            if (File.Exists(vecchio))
            {
                File.Delete(vecchio);
            }
            File.Move(attuale, vecchio);
            if (File.Exists(percorsoNuovo))
            {
                File.Delete(percorsoNuovo);
            }
            File.Move(scaricato, percorsoNuovo);

            return percorsoNuovo;
        }

        // Cancella la versione vecchia rimasta dopo un aggiornamento
        public static void TogliVersioneVecchia()
        {
            try
            {
                string vecchio = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "BatterieInBarra.vecchio.exe");
                if (File.Exists(vecchio))
                {
                    File.Delete(vecchio);
                }
            }
            catch (Exception)
            {
                // il vecchio programma si sta ancora chiudendo: riproveremo la prossima volta
            }
        }

        // GitHub vuole sapere chi sta facendo la richiesta (User-Agent)
        static HttpClient CreaClient()
        {
            HttpClient internet = new HttpClient();
            internet.Timeout = TimeSpan.FromMinutes(5);
            internet.DefaultRequestHeaders.UserAgent.ParseAdd("BatterieInBarra/" + VersioneAttuale());
            return internet;
        }
    }
}
