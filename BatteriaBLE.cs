// BatteriaBLE.cs
// Molti dispositivi Bluetooth "Low Energy" (mouse, tastiere, cuffie, smartwatch, penne...) hanno un
// "servizio batteria" standard: un piccolo valore da 0 a 100 che chiunque può leggere.
// Windows a volte non lo mostra: qui lo leggiamo noi direttamente dal dispositivo.
//
// Per non disturbare il dispositivo leggiamo al massimo ogni 2 minuti.
// Se un dispositivo non ha il servizio batteria, riproviamo solo dopo 10 minuti.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace BatterieInBarra
{
    public static class BatteriaBLE
    {
        const long OGNI_QUANTO = 2 * 60 * 1000;          // rileggiamo ogni 2 minuti
        const long OGNI_QUANTO_SE_MANCA = 10 * 60 * 1000;  // se non ha la batteria, riproviamo ogni 10 minuti
        const int ATTESA_MASSIMA = 8000;                   // 8 secondi al massimo per dispositivo

        // indirizzo -> ultimo valore letto (-1 = non ha il servizio batteria)
        static Dictionary<string, int> valori = new Dictionary<string, int>();
        // indirizzo -> quando l'abbiamo letto
        static Dictionary<string, long> quando = new Dictionary<string, long>();

        // La batteria del dispositivo con questo indirizzo (es. "A0B1C2D3E4F5"), -1 se non si sa
        public static async Task<int> Leggi(string indirizzo)
        {
            long adesso = Cronologia.Adesso();

            // Abbiamo già un valore abbastanza recente?
            if (valori.ContainsKey(indirizzo))
            {
                long attesa = valori[indirizzo] >= 0 ? OGNI_QUANTO : OGNI_QUANTO_SE_MANCA;
                if (adesso - quando[indirizzo] < attesa)
                {
                    return valori[indirizzo];
                }
            }

            int batteria = -1;
            try
            {
                // Al massimo 8 secondi: alcuni dispositivi non rispondono
                Task<int> lettura = LeggiDalDispositivo(Convert.ToUInt64(indirizzo, 16));
                if (await Task.WhenAny(lettura, Task.Delay(ATTESA_MASSIMA)) == lettura)
                {
                    batteria = lettura.Result;
                }
            }
            catch (Exception errore)
            {
                Registro.Scrivi("Batteria BLE di " + indirizzo + " non letta: " + errore.Message);
            }

            valori[indirizzo] = batteria;
            quando[indirizzo] = adesso;
            return batteria;
        }

        // Quello che abbiamo letto finora (per le Informazioni per il supporto)
        public static string Riassunto()
        {
            if (valori.Count == 0)
            {
                return "(nessun dispositivo provato)";
            }
            string testo = "";
            foreach (KeyValuePair<string, int> coppia in valori)
            {
                testo = testo + "- " + coppia.Key + " | batteria=" + (coppia.Value >= 0 ? coppia.Value + "%" : "nessun servizio batteria") + "\n";
            }
            return testo.TrimEnd();
        }

        // Si collega al dispositivo e legge il valore "livello batteria" (servizio 0x180F, valore 0x2A19)
        static async Task<int> LeggiDalDispositivo(ulong indirizzo)
        {
            using (BluetoothLEDevice dispositivo = await BluetoothLEDevice.FromBluetoothAddressAsync(indirizzo))
            {
                if (dispositivo == null)
                {
                    return -1;   // non è un dispositivo Low Energy
                }

                GattDeviceServicesResult servizi = await dispositivo.GetGattServicesForUuidAsync(GattServiceUuids.Battery, BluetoothCacheMode.Uncached);
                if (servizi.Status != GattCommunicationStatus.Success)
                {
                    return -1;
                }

                int batteria = -1;
                foreach (GattDeviceService servizio in servizi.Services)
                {
                    if (batteria < 0)
                    {
                        batteria = await LeggiDalServizio(servizio);
                    }
                    servizio.Dispose();
                }
                return batteria;
            }
        }

        static async Task<int> LeggiDalServizio(GattDeviceService servizio)
        {
            GattCharacteristicsResult valori = await servizio.GetCharacteristicsForUuidAsync(GattCharacteristicUuids.BatteryLevel, BluetoothCacheMode.Uncached);
            if (valori.Status != GattCommunicationStatus.Success || valori.Characteristics.Count == 0)
            {
                return -1;
            }

            GattReadResult lettura = await valori.Characteristics[0].ReadValueAsync(BluetoothCacheMode.Uncached);
            if (lettura.Status != GattCommunicationStatus.Success || lettura.Value == null || lettura.Value.Length == 0)
            {
                return -1;
            }

            byte[] dati = new byte[lettura.Value.Length];
            DataReader.FromBuffer(lettura.Value).ReadBytes(dati);
            return Math.Min((int)dati[0], 100);
        }
    }
}
