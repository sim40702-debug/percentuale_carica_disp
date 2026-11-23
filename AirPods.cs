// AirPods.cs
// Windows non conosce la batteria delle AirPods (e delle cuffie Beats):
// Apple la manda in un modo suo, dentro i "messaggi" Bluetooth che le AirPods trasmettono di continuo.
// Qui ascoltiamo quei messaggi e leggiamo la batteria, come fanno le app per AirPods su Windows.
//
// Il messaggio di Apple (dopo il codice dell'azienda, 0x004C) è fatto così:
//   [0] = 0x07 (tipo "vicinanza")   [1] = lunghezza
//   [6] = batteria auricolari: un numero da 0 a 10 per ogni auricolare (10 = 100%, 15 = sconosciuto)
//   [7] = la cifra bassa è la batteria della custodia (stesso sistema)

using System;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace BatterieInBarra
{
    public class LetturaAirPods
    {
        public int Auricolari;    // percentuale (quella più bassa tra sinistro e destro)
        public int Custodia;      // percentuale, -1 se non si sa (custodia chiusa o lontana)
        public bool InCarica;     // un auricolare si sta caricando (è nella custodia)
        public long Quando;       // millisecondi
        public short Segnale;     // forza del segnale: più è vicino a 0, più le AirPods sono vicine
    }

    public static class AirPods
    {
        const ushort CODICE_APPLE = 0x004C;
        const short SEGNALE_MINIMO = -90;     // più debole di così = troppo lontane (con -70 a volte scartavamo anche le tue)
        const long VALIDITA = 5 * 60 * 1000;  // una lettura vale 5 minuti (le AirPods nelle orecchie mandano meno messaggi)

        static BluetoothLEAdvertisementWatcher ascolto = null;
        static LetturaAirPods ultima = null;
        static readonly object lucchetto = new object();   // i messaggi arrivano da un altro "thread"

        // Contatori per le Informazioni per il supporto
        static int messaggiApple = 0;       // messaggi di Apple ricevuti
        static int messaggiAirPods = 0;     // di questi, quanti avevano la batteria delle AirPods
        static int scartatiDeboli = 0;      // scartati perché il segnale era troppo debole
        static short segnaleMigliore = -127;

        // Inizia ad ascoltare (solo quando ci sono AirPods collegate, così non usa CPU per niente)
        public static void Avvia()
        {
            if (ascolto != null)
            {
                // Se Windows ha fermato l'ascolto (Bluetooth spento e riacceso, PC in sospensione...) lo ripartiamo
                bool fermo = ascolto.Status == BluetoothLEAdvertisementWatcherStatus.Aborted
                          || ascolto.Status == BluetoothLEAdvertisementWatcherStatus.Stopped;
                if (!fermo)
                {
                    return;
                }
                Registro.Scrivi("AirPods: l'ascolto si era fermato (" + ascolto.Status + "), lo riavvio");
                Ferma();
            }
            try
            {
                ascolto = new BluetoothLEAdvertisementWatcher();
                ascolto.ScanningMode = BluetoothLEScanningMode.Active;   // con "Active" alcuni PC ricevono più messaggi
                ascolto.Received += QuandoArrivaMessaggio;
                ascolto.Start();
            }
            catch (Exception errore)
            {
                Registro.Scrivi("AirPods: impossibile ascoltare il Bluetooth: " + errore.Message);
                ascolto = null;   // Bluetooth spento o non disponibile
            }
        }

        public static void Ferma()
        {
            if (ascolto == null)
            {
                return;
            }
            try
            {
                ascolto.Received -= QuandoArrivaMessaggio;
                ascolto.Stop();
            }
            catch (Exception)
            {
            }
            ascolto = null;
        }

        // L'ultima lettura, se è recente (altrimenti null)
        public static LetturaAirPods Ultima()
        {
            lock (lucchetto)
            {
                if (ultima != null && Cronologia.Adesso() - ultima.Quando < VALIDITA)
                {
                    return ultima;
                }
                return null;
            }
        }

        // Com'è andato l'ascolto (per le Informazioni per il supporto)
        public static string Stato()
        {
            lock (lucchetto)
            {
                string stato = ascolto == null ? "spento" : ascolto.Status.ToString();
                return "Ascolto: " + stato + " | messaggi Apple: " + messaggiApple + " | con batteria AirPods: " + messaggiAirPods
                    + " | scartati (segnale debole): " + scartatiDeboli + " | segnale migliore: " + segnaleMigliore + " dBm";
            }
        }

        // Legge la batteria da un messaggio di Apple. Restituisce null se non è un messaggio delle AirPods.
        public static LetturaAirPods LeggiMessaggio(byte[] b, short segnale)
        {
            // Deve essere un messaggio di tipo "vicinanza" (0x07) abbastanza lungo
            if (b.Length < 8 || b[0] != 0x07)
            {
                return null;
            }

            int primo = b[6] >> 4;       // un auricolare
            int secondo = b[6] & 0x0F;   // l'altro auricolare
            int custodia = b[7] & 0x0F;

            // Prendiamo l'auricolare più scarico (15 = sconosciuto, es. è nella custodia)
            int auricolari = -1;
            if (primo <= 10)
            {
                auricolari = primo;
            }
            if (secondo <= 10 && (auricolari < 0 || secondo < auricolari))
            {
                auricolari = secondo;
            }
            if (auricolari < 0)
            {
                return null;
            }

            LetturaAirPods lettura = new LetturaAirPods();
            lettura.Auricolari = auricolari * 10;
            lettura.Custodia = custodia <= 10 ? custodia * 10 : -1;
            // Cifra alta del byte 7: bit 1 e 2 = gli auricolari si stanno caricando (bit 4 = la custodia)
            lettura.InCarica = (b[7] & 0x30) != 0;
            lettura.Quando = Cronologia.Adesso();
            lettura.Segnale = segnale;
            return lettura;
        }

        static void QuandoArrivaMessaggio(BluetoothLEAdvertisementWatcher chi, BluetoothLEAdvertisementReceivedEventArgs messaggio)
        {
            short segnale = messaggio.RawSignalStrengthInDBm;

            foreach (BluetoothLEManufacturerData dati in messaggio.Advertisement.ManufacturerData)
            {
                if (dati.CompanyId != CODICE_APPLE)
                {
                    continue;
                }

                byte[] b = new byte[dati.Data.Length];
                DataReader.FromBuffer(dati.Data).ReadBytes(b);

                LetturaAirPods nuova = LeggiMessaggio(b, segnale);

                lock (lucchetto)
                {
                    messaggiApple = messaggiApple + 1;
                    if (nuova == null)
                    {
                        continue;
                    }
                    messaggiAirPods = messaggiAirPods + 1;
                    if (segnale > segnaleMigliore)
                    {
                        segnaleMigliore = segnale;
                    }
                    if (segnale < SEGNALE_MINIMO)
                    {
                        scartatiDeboli = scartatiDeboli + 1;
                        continue;
                    }

                    // Se ci sono più AirPods vicine, teniamo quelle col segnale più forte (le tue)
                    bool vecchia = ultima == null || nuova.Quando - ultima.Quando > 15000;
                    if (vecchia || nuova.Segnale >= ultima.Segnale - 5)
                    {
                        ultima = nuova;
                    }
                }
            }
        }
    }
}
