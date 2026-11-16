// Aggiornamenti.cs
// Controlla su GitHub se c'è una versione nuova, la scarica e (ad app chiusa) sostituisce quella vecchia.
// I dati (impostazioni e cronologia) stanno in %APPDATA%\BatterieInBarra: l'aggiornamento non li tocca.

using System;
using System.Diagnostics;
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

        // La pagina dell'ultima release (per scaricarla a mano se l'aggiornamento automatico non riesce)
        public static string PaginaRelease()
        {
            return "https://github.com/" + REPOSITORY + "/releases/latest";
        }

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

        // Scarica la versione nuova accanto a quella attuale, con il nome "BatterieInBarra.nuovo.exe".
        // Restituisce il percorso del file scaricato.
        // (Il cambio vero lo fa un piccolo script DOPO che l'app si è chiusa: vedi PreparaCambio)
        public static async Task<string> Scarica(NuovaVersione nuova)
        {
            string cartella = Path.GetDirectoryName(Environment.ProcessPath);
            string scaricato = Path.Combine(cartella, "BatterieInBarra.nuovo.exe");

            using (HttpClient internet = CreaClient())
            {
                byte[] dati = await internet.GetByteArrayAsync(nuova.Indirizzo);

                // Controllo: un exe vero pesa decine di MB, non pochi KB (es. una pagina di errore)
                if (dati.Length < 1024 * 1024)
                {
                    throw new Exception("Il file scaricato è troppo piccolo (" + dati.Length + " byte)");
                }
                await File.WriteAllBytesAsync(scaricato, dati);
            }
            return scaricato;
        }

        // Prepara e avvia un piccolo script che:
        // 1. aspetta che questa app si chiuda del tutto
        // 2. mette la versione nuova al posto di quella vecchia (stesso nome, stessa cartella)
        // 3. avvia la versione nuova
        // 4. cancella se stesso
        // Così non si cambia mai un programma mentre è acceso.
        public static void PreparaCambio(string scaricato)
        {
            string attuale = Environment.ProcessPath;
            int numeroProcesso = Environment.ProcessId;
            string script = Path.Combine(Path.GetTempPath(), "BatterieInBarra-aggiorna.cmd");

            string[] righe = new string[]
            {
                "@echo off",
                "chcp 65001 >nul",
                ":aspetta",
                "tasklist /fi \"PID eq " + numeroProcesso + "\" | find \" " + numeroProcesso + " \" >nul && (ping -n 2 127.0.0.1 >nul & goto aspetta)",
                "move /y \"" + scaricato + "\" \"" + attuale + "\" >nul",
                "start \"\" \"" + attuale + "\" --aggiornato",
                "del \"%~f0\""
            };
            File.WriteAllText(script, string.Join("\r\n", righe) + "\r\n", new System.Text.UTF8Encoding(false));

            ProcessStartInfo avvio = new ProcessStartInfo("cmd.exe", "/c \"" + script + "\"");
            avvio.CreateNoWindow = true;
            avvio.UseShellExecute = false;
            Process.Start(avvio);
        }

        // Cancella i file rimasti da aggiornamenti vecchi o non finiti
        public static void TogliVersioneVecchia()
        {
            string cartella = Path.GetDirectoryName(Environment.ProcessPath);
            string[] avanzi = new string[] { "BatterieInBarra.vecchio.exe", "BatterieInBarra.nuovo.exe" };
            foreach (string nome in avanzi)
            {
                try
                {
                    string file = Path.Combine(cartella, nome);
                    if (File.Exists(file) && !string.Equals(file, Environment.ProcessPath, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(file);
                    }
                }
                catch (Exception)
                {
                    // è ancora in uso: riproveremo la prossima volta
                }
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
