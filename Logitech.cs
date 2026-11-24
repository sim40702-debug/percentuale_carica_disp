// Logitech.cs
// Mouse e tastiere Logitech con la chiavetta (Unifying, Bolt, Lightspeed) o in Bluetooth.
// Windows non sa la loro batteria: Logitech usa un suo "linguaggio" chiamato HID++.
// Gli mandiamo delle domande (messaggi da 20 byte) e lui risponde con la batteria e se è in carica,
// come fanno Logitech Options e Solaar.
//
// Come funziona una domanda:
//   [0x11] [numero del dispositivo] [funzione] [cosa chiediamo] [dati...]
// - numero del dispositivo: 1-6 sulla chiavetta, 0xFF in Bluetooth
// - le funzioni hanno un codice fisso (es. 0x1004 = batteria), ma ogni dispositivo le tiene in una
//   "posizione" diversa: prima chiediamo la posizione (funzione 0, "root"), poi usiamo quella.
//
// Funzioni che usiamo:
// - 0x1004 "batteria unificata" (dispositivi nuovi): percentuale e stato di carica
// - 0x1000 "stato batteria" (dispositivi un po' più vecchi): percentuale e stato
// - 0x1001 "voltaggio batteria" (es. mouse da gioco G): solo i volt, la percentuale è una stima
// - 0x0005 "nome e tipo": per sapere come si chiama e se è un mouse o una tastiera
//
// La lettura è lenta (un dispositivo spento ci fa aspettare): la facciamo in sottofondo ogni minuto
// e intanto l'app usa l'ultimo risultato.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;

namespace BatterieInBarra
{
    public class LogitechTrovato
    {
        public string Nome;
        public string Tipo;        // "mouse", "tastiera" o "" se non si sa
        public int Batteria;
        public bool InCarica;
        public long Quando;        // quando l'abbiamo letto
    }

    public static class Logitech
    {
        const ushort LOGITECH = 0x046D;
        const byte LUNGO = 0x11;            // i messaggi "lunghi" da 20 byte
        const byte CHI_SIAMO = 0x0A;        // un numero nostro, per riconoscere le risposte alle nostre domande
        const long OGNI_QUANTO = 60 * 1000;          // rileggiamo ogni minuto
        const long VALIDITA = 10 * 60 * 1000;        // se un dispositivo dorme, teniamo l'ultima lettura per 10 minuti
        const int ATTESA_RISPOSTA = 500;             // mezzo secondo per ogni risposta

        static List<LogitechTrovato> ultimi = new List<LogitechTrovato>();
        static long ultimaLettura = -OGNI_QUANTO;
        static bool staLeggendo = false;
        static readonly object lucchetto = new object();
        static string riassunto = "(non ancora letto)";

        // ----- Funzioni di Windows -----

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern SafeFileHandle CreateFile(string percorso, uint accesso, uint condivisione, IntPtr sicurezza,
            uint creazione, uint opzioni, IntPtr modello);

        const uint GENERIC_READ = 0x80000000;
        const uint GENERIC_WRITE = 0x40000000;
        const uint FILE_SHARE_READ = 0x1;
        const uint FILE_SHARE_WRITE = 0x2;
        const uint OPEN_EXISTING = 3;
        const uint FILE_FLAG_OVERLAPPED = 0x40000000;


        // ----- Da usare nell'app -----

        // I dispositivi Logitech trovati. Risponde subito con l'ultimo risultato
        // e, se è passato più di un minuto, fa partire una lettura nuova in sottofondo.
        public static List<LogitechTrovato> Leggi()
        {
            long adesso = Cronologia.Adesso();
            List<LogitechTrovato> risultato = new List<LogitechTrovato>();

            lock (lucchetto)
            {
                if (!staLeggendo && adesso - ultimaLettura >= OGNI_QUANTO)
                {
                    staLeggendo = true;
                    ultimaLettura = adesso;
                    Task.Run(LeggiInSottofondo);
                }

                foreach (LogitechTrovato t in ultimi)
                {
                    if (adesso - t.Quando < VALIDITA)
                    {
                        risultato.Add(t);
                    }
                }
            }
            return risultato;
        }

        // Per le Informazioni per il supporto
        public static string Riassunto()
        {
            lock (lucchetto)
            {
                return riassunto;
            }
        }

        static async Task LeggiInSottofondo()
        {
            List<LogitechTrovato> nuovi = new List<LogitechTrovato>();
            StringBuilder testo = new StringBuilder();
            try
            {
                // Chiavette (Unifying, Bolt, Lightspeed): dispositivi da 1 a 6
                await Cerca(0xFF00, 0x0002, true, nuovi, testo);
                // Bluetooth diretto: dispositivo 0xFF
                await Cerca(0xFF43, 0x0202, false, nuovi, testo);
            }
            catch (Exception errore)
            {
                testo.AppendLine("Errore: " + errore.Message);
            }

            lock (lucchetto)
            {
                // Teniamo anche quelli letti prima che adesso non rispondono (dormono): valgono 10 minuti
                foreach (LogitechTrovato vecchio in ultimi)
                {
                    bool ancoraQui = false;
                    foreach (LogitechTrovato nuovo in nuovi)
                    {
                        if (nuovo.Nome == vecchio.Nome)
                        {
                            ancoraQui = true;
                        }
                    }
                    if (!ancoraQui)
                    {
                        nuovi.Add(vecchio);
                    }
                }
                ultimi = nuovi;
                riassunto = testo.Length > 0 ? testo.ToString().TrimEnd() : "(nessuna chiavetta o dispositivo Logitech)";
                staLeggendo = false;
            }
        }

        static async Task Cerca(ushort pagina, ushort uso, bool chiavetta, List<LogitechTrovato> trovati, StringBuilder testo)
        {
            string filtro = HidDevice.GetDeviceSelector(pagina, uso);
            foreach (DeviceInformation d in await DeviceInformation.FindAllAsync(filtro))
            {
                if (!d.Id.ToUpper().Contains("VID_046D"))
                {
                    continue;   // non è Logitech
                }
                testo.AppendLine((chiavetta ? "Chiavetta: " : "Bluetooth: ") + d.Name);
                await LeggiDa(d.Id, chiavetta, trovati, testo);
            }
        }

        static async Task LeggiDa(string percorso, bool chiavetta, List<LogitechTrovato> trovati, StringBuilder testo)
        {
            SafeFileHandle maniglia = CreateFile(percorso, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, IntPtr.Zero);
            if (maniglia.IsInvalid)
            {
                testo.AppendLine("  non si apre (errore " + Marshal.GetLastWin32Error() + ")");
                maniglia.Dispose();
                return;
            }

            Canale canale = new Canale(new FileStream(maniglia, FileAccess.ReadWrite, 20, true));
            try
            {
                List<byte> numeri = new List<byte>();
                if (chiavetta)
                {
                    for (byte n = 1; n <= 6; n++)
                    {
                        numeri.Add(n);
                    }
                }
                else
                {
                    numeri.Add(0xFF);
                }

                foreach (byte numero in numeri)
                {
                    LogitechTrovato t = await LeggiDispositivo(canale, numero);
                    if (t != null)
                    {
                        testo.AppendLine("  " + numero + ": " + t.Nome + " | " + t.Tipo + " | batteria=" + t.Batteria + " | in carica=" + t.InCarica);
                        trovati.Add(t);
                    }
                }
            }
            catch (Exception errore)
            {
                testo.AppendLine("  errore: " + errore.Message);
            }
            finally
            {
                canale.Chiudi();
            }
        }

        // Legge un dispositivo (null se non c'è, dorme o non dice la batteria)
        static async Task<LogitechTrovato> LeggiDispositivo(Canale canale, byte numero)
        {
            // In che posizione ha la funzione "batteria unificata"? (null = il dispositivo non risponde)
            int unificata = await Posizione(canale, numero, 0x1004);
            if (unificata < 0)
            {
                return null;
            }

            LogitechTrovato t = new LogitechTrovato();
            t.Quando = Cronologia.Adesso();
            t.Batteria = -1;

            if (unificata > 0)
            {
                byte[] r = await canale.Chiedi(numero, (byte)unificata, 1);
                if (r != null)
                {
                    t.Batteria = PercentualeUnificata(r[0], r[1]);
                    t.InCarica = InCaricaUnificata(r[2]);
                }
            }
            else
            {
                int stato = await Posizione(canale, numero, 0x1000);
                if (stato > 0)
                {
                    byte[] r = await canale.Chiedi(numero, (byte)stato, 0);
                    if (r != null)
                    {
                        t.Batteria = Math.Min((int)r[0], 100);
                        t.InCarica = InCaricaStato(r[2]);
                    }
                }
                else
                {
                    int volt = await Posizione(canale, numero, 0x1001);
                    if (volt > 0)
                    {
                        byte[] r = await canale.Chiedi(numero, (byte)volt, 0);
                        if (r != null)
                        {
                            t.Batteria = DaVoltaggio((r[0] << 8) | r[1]);
                            t.InCarica = (r[2] & 0x80) != 0;
                        }
                    }
                }
            }

            if (t.Batteria < 0)
            {
                return null;
            }

            // Nome e tipo
            t.Nome = "Logitech " + numero;
            t.Tipo = "";
            int nome = await Posizione(canale, numero, 0x0005);
            if (nome > 0)
            {
                string letto = await LeggiNome(canale, numero, (byte)nome);
                if (letto != "")
                {
                    t.Nome = letto;
                }
                byte[] r = await canale.Chiedi(numero, (byte)nome, 2);
                if (r != null)
                {
                    t.Tipo = TipoDa(r[0]);
                }
            }
            return t;
        }

        // La posizione di una funzione: 0 = non ce l'ha, -1 = il dispositivo non risponde
        static async Task<int> Posizione(Canale canale, byte numero, int codice)
        {
            byte[] r = await canale.Chiedi(numero, 0x00, 0, (byte)(codice >> 8), (byte)(codice & 0xFF));
            if (r == null)
            {
                return -1;
            }
            return r[0];
        }

        static async Task<string> LeggiNome(Canale canale, byte numero, byte posizione)
        {
            byte[] r = await canale.Chiedi(numero, posizione, 0);
            if (r == null)
            {
                return "";
            }
            int lunghezza = r[0];
            StringBuilder nome = new StringBuilder();
            while (nome.Length < lunghezza)
            {
                byte[] pezzo = await canale.Chiedi(numero, posizione, 1, (byte)nome.Length);
                if (pezzo == null)
                {
                    break;
                }
                int prima = nome.Length;
                for (int i = 0; i < pezzo.Length && nome.Length < lunghezza; i++)
                {
                    if (pezzo[i] == 0)
                    {
                        break;
                    }
                    nome.Append((char)pezzo[i]);
                }
                if (nome.Length == prima)
                {
                    break;   // non arriva niente di nuovo
                }
            }
            return nome.ToString().Trim();
        }


        // ----- Calcoli (public per i test automatici) -----

        // Funzione 0x1004: percentuale, oppure (se il dispositivo non la dice) il livello
        // (bit: 8 = piena, 4 = buona, 2 = bassa, 1 = quasi scarica)
        public static int PercentualeUnificata(byte percentuale, byte livello)
        {
            if (percentuale > 0)
            {
                return Math.Min((int)percentuale, 100);
            }
            if ((livello & 8) != 0) return 90;
            if ((livello & 4) != 0) return 50;
            if ((livello & 2) != 0) return 20;
            if ((livello & 1) != 0) return 5;
            return 0;
        }

        // Funzione 0x1004: 1 = in carica, 2 = in carica lenta (3 = completa, 0 = a batteria)
        public static bool InCaricaUnificata(byte stato)
        {
            return stato == 1 || stato == 2;
        }

        // Funzione 0x1000: 1 = in carica, 2 = quasi carica, 4 = carica lenta (3 = completa, 0 = a batteria)
        public static bool InCaricaStato(byte stato)
        {
            return stato == 1 || stato == 2 || stato == 4;
        }

        // Funzione 0x1001: dai millivolt una percentuale (stima: 3,5 V = vuota, 4,1 V = piena)
        public static int DaVoltaggio(int millivolt)
        {
            int percentuale = (millivolt - 3500) * 100 / 600;
            return Math.Max(0, Math.Min(percentuale, 100));
        }

        // Funzione 0x0005: 0 = tastiera, 2 = tastierino, 3 = mouse, 4 = touchpad, 5 = trackball
        public static string TipoDa(byte tipo)
        {
            if (tipo == 0 || tipo == 2) return "tastiera";
            if (tipo == 3 || tipo == 4 || tipo == 5) return "mouse";
            return "";
        }


        // ----- Il "canale": manda una domanda e aspetta la sua risposta -----

        class Canale
        {
            FileStream flusso;
            Task<int> letturaInCorso = null;   // una lettura alla volta: se scade, la riusiamo alla domanda dopo
            byte[] buffer = new byte[20];

            public Canale(FileStream flusso)
            {
                this.flusso = flusso;
            }

            // Restituisce i dati della risposta (16 byte), null se non risponde o dà errore
            public async Task<byte[]> Chiedi(byte numero, byte posizione, int funzione, params byte[] dati)
            {
                byte[] domanda = new byte[20];
                domanda[0] = LUNGO;
                domanda[1] = numero;
                domanda[2] = posizione;
                domanda[3] = (byte)((funzione << 4) | CHI_SIAMO);
                for (int i = 0; i < dati.Length && i < 16; i++)
                {
                    domanda[4 + i] = dati[i];
                }
                await flusso.WriteAsync(domanda, 0, domanda.Length);

                // Arrivano anche altri messaggi (es. movimenti): aspettiamo quello giusto, al massimo mezzo secondo
                long fine = Cronologia.Adesso() + ATTESA_RISPOSTA;
                while (Cronologia.Adesso() < fine)
                {
                    byte[] r = await Prossimo((int)(fine - Cronologia.Adesso()));
                    if (r == null)
                    {
                        return null;
                    }
                    if (r.Length < 5 || r[0] != LUNGO || r[1] != numero)
                    {
                        continue;
                    }
                    // Errore: [0x11][numero][0xFF][posizione][funzione][codice errore]
                    if (r[2] == 0xFF && r[3] == posizione && r[4] == domanda[3])
                    {
                        return null;
                    }
                    if (r[2] == posizione && r[3] == domanda[3])
                    {
                        byte[] risposta = new byte[16];
                        Array.Copy(r, 4, risposta, 0, Math.Min(16, r.Length - 4));
                        return risposta;
                    }
                }
                return null;
            }

            async Task<byte[]> Prossimo(int millisecondi)
            {
                if (millisecondi <= 0)
                {
                    return null;
                }
                if (letturaInCorso == null)
                {
                    buffer = new byte[20];
                    letturaInCorso = flusso.ReadAsync(buffer, 0, buffer.Length);
                }
                if (await Task.WhenAny(letturaInCorso, Task.Delay(millisecondi)) != letturaInCorso)
                {
                    return null;   // la lettura resta in corso: la usiamo alla prossima domanda
                }
                int letti = letturaInCorso.Result;
                letturaInCorso = null;
                byte[] r = new byte[letti];
                Array.Copy(buffer, r, letti);
                return r;
            }

            public void Chiudi()
            {
                // Se una lettura è ancora in corso, chiudendo dà errore: lo "guardiamo" così non disturba
                if (letturaInCorso != null)
                {
                    letturaInCorso.ContinueWith(Ignora, TaskContinuationOptions.OnlyOnFaulted);
                }
                try
                {
                    flusso.Dispose();
                }
                catch (Exception)
                {
                }
            }

            static void Ignora(Task<int> lettura)
            {
                AggregateException errore = lettura.Exception;
            }
        }
    }
}
