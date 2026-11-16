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
        public long Quando;       // millisecondi
        public short Segnale;     // forza del segnale: più è vicino a 0, più le AirPods sono vicine
    }

    public static class AirPods
    {
        const ushort CODICE_APPLE = 0x004C;
        const short SEGNALE_MINIMO = -70;     // più debole di così = AirPods di qualcun altro, lontane
        const long VALIDITA = 90 * 1000;      // una lettura vale 90 secondi

        static BluetoothLEAdvertisementWatcher ascolto = null;
        static LetturaAirPods ultima = null;
        static readonly object lucchetto = new object();   // i messaggi arrivano da un altro "thread"

        // Inizia ad ascoltare (solo quando ci sono AirPods collegate, così non usa CPU per niente)
        public static void Avvia()
        {
            if (ascolto != null)
            {
                return;
            }
            try
            {
                ascolto = new BluetoothLEAdvertisementWatcher();
                ascolto.ScanningMode = BluetoothLEScanningMode.Passive;
                ascolto.Received += QuandoArrivaMessaggio;
                ascolto.Start();
            }
            catch (Exception)
            {
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
            lettura.Quando = Cronologia.Adesso();
            lettura.Segnale = segnale;
            return lettura;
        }

        static void QuandoArrivaMessaggio(BluetoothLEAdvertisementWatcher chi, BluetoothLEAdvertisementReceivedEventArgs messaggio)
        {
            if (messaggio.RawSignalStrengthInDBm < SEGNALE_MINIMO)
            {
                return;
            }

            foreach (BluetoothLEManufacturerData dati in messaggio.Advertisement.ManufacturerData)
            {
                if (dati.CompanyId != CODICE_APPLE)
                {
                    continue;
                }

                byte[] b = new byte[dati.Data.Length];
                DataReader.FromBuffer(dati.Data).ReadBytes(b);

                LetturaAirPods nuova = LeggiMessaggio(b, messaggio.RawSignalStrengthInDBm);
                if (nuova == null)
                {
                    continue;
                }

                lock (lucchetto)
                {
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
