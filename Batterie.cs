// Batterie.cs
// Chiede a Windows la batteria dei dispositivi Bluetooth COLLEGATI.
// Usa direttamente le funzioni di Windows (niente PowerShell): è molto più leggero.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace BatterieInBarra
{
    // Un dispositivo letto da Windows
    public class Dispositivo
    {
        public string Nome;      // nome originale (es. "WH-1000XM5")
        public int Batteria;     // percentuale (0-100)
    }

    public static class Batterie
    {
        // I "codici" con cui Windows salva le informazioni dei dispositivi
        const string CHIAVE_COLLEGATO = "{83DA6326-97A6-4088-9453-A1923F573B29} 15";
        const string CHIAVE_BATTERIA = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";

        // Filtri per cercare i dispositivi
        const string FILTRO_BLUETOOTH = "System.Devices.ClassGuid:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\"";
        const string FILTRO_BTH = "System.Devices.DeviceInstanceId:~<\"BTH\"";
        const string FILTRO_PRESENTI = "System.Devices.Present:=System.StructuredQueryType.Boolean#True";

        public static async Task<List<Dispositivo>> Leggi()
        {
            List<Dispositivo> lista = new List<Dispositivo>();

            // 1. Dispositivi Bluetooth COLLEGATI adesso: indirizzo (MAC) -> nome
            Dictionary<string, string> collegati = new Dictionary<string, string>();

            DeviceInformationCollection bluetooth = await DeviceInformation.FindAllAsync(
                FILTRO_BLUETOOTH, new string[] { CHIAVE_COLLEGATO }, DeviceInformationKind.Device);

            foreach (DeviceInformation d in bluetooth)
            {
                Match indirizzo = Regex.Match(d.Id.ToUpper(), "DEV_([0-9A-F]{12})");
                if (!indirizzo.Success)
                {
                    continue;
                }

                object stato = LeggiProprieta(d, CHIAVE_COLLEGATO);
                if (stato is bool && (bool)stato)
                {
                    collegati[indirizzo.Groups[1].Value] = d.Name;
                }
            }

            if (collegati.Count == 0)
            {
                return lista;
            }

            // 2. La batteria, ma solo dei dispositivi collegati
            DeviceInformationCollection dispositivi = await CercaDispositiviBluetooth();
            List<string> nomiVisti = new List<string>();

            foreach (DeviceInformation d in dispositivi)
            {
                string id = d.Id.ToUpper();
                if (!id.StartsWith("BTH"))
                {
                    continue;
                }

                object valore = LeggiProprieta(d, CHIAVE_BATTERIA);
                if (valore == null)
                {
                    continue;
                }
                int batteria = Convert.ToInt32(valore);

                // A quale dispositivo collegato appartiene? (l'indirizzo è dentro l'Id)
                foreach (KeyValuePair<string, string> coppia in collegati)
                {
                    string nome = coppia.Value;
                    if (id.Contains(coppia.Key) && !nomiVisti.Contains(nome))
                    {
                        nomiVisti.Add(nome);
                        lista.Add(new Dispositivo { Nome = nome, Batteria = batteria });
                    }
                }
            }

            return lista;
        }

        // Cerca i dispositivi il cui Id inizia con "BTH".
        // Se il filtro veloce non funziona, prende tutti quelli presenti (li filtriamo dopo).
        static async Task<DeviceInformationCollection> CercaDispositiviBluetooth()
        {
            string[] proprieta = new string[] { CHIAVE_BATTERIA };
            try
            {
                return await DeviceInformation.FindAllAsync(FILTRO_BTH, proprieta, DeviceInformationKind.Device);
            }
            catch (Exception)
            {
                return await DeviceInformation.FindAllAsync(FILTRO_PRESENTI, proprieta, DeviceInformationKind.Device);
            }
        }

        // Legge una proprietà del dispositivo (null se non c'è)
        static object LeggiProprieta(DeviceInformation d, string chiave)
        {
            object valore;
            if (d.Properties.TryGetValue(chiave, out valore))
            {
                return valore;
            }

            // A volte Windows scrive la chiave in minuscolo: proviamo anche così
            foreach (KeyValuePair<string, object> coppia in d.Properties)
            {
                if (string.Equals(coppia.Key, chiave, StringComparison.OrdinalIgnoreCase))
                {
                    return coppia.Value;
                }
            }
            return null;
        }
    }
}
