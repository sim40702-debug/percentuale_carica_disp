// Batterie.cs
// Chiede a Windows la batteria dei dispositivi Bluetooth COLLEGATI.
// Usa direttamente le funzioni di Windows: è molto più leggero.
// Solo se un dispositivo collegato non si trova così, usa come riserva il metodo della prima versione (PowerShell).

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace BatterieInBarra
{
    // Un dispositivo letto da Windows
    public class Dispositivo
    {
        public string Nome;      // nome originale (es. "WH-1000XM5")
        public int Batteria;     // percentuale (0-100)
        public bool SaSeCarica;  // il dispositivo ci dice se è in carica? (controller PlayStation e AirPods sì)
        public bool InCarica;    // si sta caricando (vale solo se SaSeCarica è true)
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
            Dictionary<string, string> collegati = await TrovaCollegati();

            if (collegati.Count == 0)
            {
                AirPods.Ferma();

                // Controller PlayStation col cavo (non sono Bluetooth)
                await AggiungiPlayStation(collegati, lista, new List<string>());

                // Nessun collegato trovato: proviamo comunque la riserva, ma solo ogni 10 minuti
                await AggiungiDaRiserva(collegati, lista, new List<string>());
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

            // 3. Controller PlayStation: Windows non sa la batteria, la leggiamo dai dati del controller
            await AggiungiPlayStation(collegati, lista, nomiVisti);

            // 4. Riserva: se un dispositivo collegato non ha ancora la batteria, proviamo il metodo
            //    della prima versione (PowerShell). Su alcuni PC trova dispositivi che l'altro non vede.
            await AggiungiDaRiserva(collegati, lista, nomiVisti);

            // 5. AirPods e Beats: Windows non sa la batteria, la leggiamo dai messaggi di Apple
            AggiungiAirPods(collegati, lista, nomiVisti);

            return lista;
        }

        // ----- Lettura di riserva (il metodo della prima versione, con PowerShell) -----

        const long OGNI_QUANTO_RISERVA = 2 * 60 * 1000;   // al massimo ogni 2 minuti
        static long ultimaRiserva = 0;
        static Dictionary<string, int> risultatiRiserva = new Dictionary<string, int>();

        // Lo stesso comando che usava la versione in Electron (che su quel PC trovava il mouse)
        const string COMANDO_RISERVA = @"
$CHIAVE_COLLEGATO = '{83DA6326-97A6-4088-9453-A1923F573B29} 15'
$CHIAVE_BATTERIA = '{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2'
$collegati = @{}
foreach ($d in Get-PnpDevice -Class Bluetooth) {
  if ($d.InstanceId -match 'DEV_([0-9A-F]{12})') {
    $indirizzo = $Matches[1]
    $stato = (Get-PnpDeviceProperty -InstanceId $d.InstanceId -KeyName $CHIAVE_COLLEGATO -ErrorAction SilentlyContinue).Data
    if ($stato -eq $true) { $collegati[$indirizzo] = $d.FriendlyName }
  }
}
$lista = @()
if ($collegati.Count -gt 0) {
  foreach ($d in Get-PnpDevice | Where-Object { $_.InstanceId -like 'BTH*' }) {
    $batteria = (Get-PnpDeviceProperty -InstanceId $d.InstanceId -KeyName $CHIAVE_BATTERIA -ErrorAction SilentlyContinue).Data
    if ($batteria -ne $null) {
      foreach ($indirizzo in $collegati.Keys) {
        if ($d.InstanceId -like ""*$indirizzo*"") {
          $lista += [PSCustomObject]@{ nome = $collegati[$indirizzo]; batteria = [int]$batteria }
        }
      }
    }
  }
}
ConvertTo-Json -InputObject $lista -Compress
";

        static async Task AggiungiDaRiserva(Dictionary<string, string> collegati, List<Dispositivo> lista, List<string> nomiVisti)
        {
            // Quali dispositivi collegati non hanno ancora la batteria? (AirPods e Beats a parte)
            // Se non abbiamo trovato nessun collegato, proviamo lo stesso (ma più di rado)
            bool mancaQualcuno = collegati.Count == 0;
            foreach (string nome in collegati.Values)
            {
                string n = nome.ToLower();
                if (!nomiVisti.Contains(nome) && !n.Contains("airpods") && !n.Contains("beats"))
                {
                    mancaQualcuno = true;
                }
            }
            if (!mancaQualcuno)
            {
                return;
            }

            // Rifacciamo la lettura solo ogni 2 minuti (ogni 10 se non c'è nessun collegato): PowerShell pesa un po'
            long intervallo = collegati.Count == 0 ? 5 * OGNI_QUANTO_RISERVA : OGNI_QUANTO_RISERVA;
            if (Cronologia.Adesso() - ultimaRiserva > intervallo)
            {
                ultimaRiserva = Cronologia.Adesso();
                risultatiRiserva = await LeggiConPowerShell();
            }

            foreach (KeyValuePair<string, int> coppia in risultatiRiserva)
            {
                // (PowerShell controlla già da solo che il dispositivo sia collegato)
                if (!nomiVisti.Contains(coppia.Key))
                {
                    nomiVisti.Add(coppia.Key);
                    lista.Add(new Dispositivo { Nome = coppia.Key, Batteria = coppia.Value });
                }
            }
        }

        // Esegue il comando PowerShell e restituisce nome -> batteria
        public static async Task<Dictionary<string, int>> LeggiConPowerShell()
        {
            Dictionary<string, int> risultato = new Dictionary<string, int>();
            try
            {
                // PowerShell vuole il comando "codificato" quando è su più righe
                string codificato = Convert.ToBase64String(Encoding.Unicode.GetBytes(COMANDO_RISERVA));

                ProcessStartInfo avvio = new ProcessStartInfo("powershell.exe",
                    "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + codificato);
                avvio.CreateNoWindow = true;
                avvio.UseShellExecute = false;
                avvio.RedirectStandardOutput = true;

                using (Process powershell = Process.Start(avvio))
                {
                    Task<string> uscita = powershell.StandardOutput.ReadToEndAsync();
                    Task finito = Task.WhenAll(uscita, powershell.WaitForExitAsync());

                    // Al massimo 30 secondi
                    if (await Task.WhenAny(finito, Task.Delay(30000)) != finito)
                    {
                        powershell.Kill();
                        Registro.Scrivi("Lettura di riserva: PowerShell troppo lento, fermato");
                        return risultato;
                    }

                    string testo = uscita.Result.Trim();
                    if (testo == "")
                    {
                        return risultato;
                    }

                    using (JsonDocument json = JsonDocument.Parse(testo))
                    {
                        foreach (JsonElement elemento in json.RootElement.EnumerateArray())
                        {
                            string nome = elemento.GetProperty("nome").GetString();
                            int batteria = elemento.GetProperty("batteria").GetInt32();
                            if (nome != null && !risultato.ContainsKey(nome))
                            {
                                risultato[nome] = batteria;
                            }
                        }
                    }
                }
            }
            catch (Exception errore)
            {
                Registro.Scrivi("Lettura di riserva non riuscita: " + errore.Message);
            }
            return risultato;
        }

        // Controller PS4 e PS5: il nome lo prendiamo dal Bluetooth ("Wireless Controller", "DualSense..."),
        // altrimenti (col cavo) usiamo "Controller PS4" / "Controller PS5"
        static async Task AggiungiPlayStation(Dictionary<string, string> collegati, List<Dispositivo> lista, List<string> nomiVisti)
        {
            List<ControllerTrovato> controller = await PlayStation.Leggi();
            int numero = 0;

            foreach (ControllerTrovato c in controller)
            {
                numero = numero + 1;

                // Cerchiamo il nome Bluetooth giusto
                string nome = c.EPS5 ? "Controller PS5" : "Controller PS4";
                foreach (string collegato in collegati.Values)
                {
                    string n = collegato.ToLower();
                    bool somiglia = c.EPS5 ? n.Contains("dualsense") : n.Contains("wireless controller");
                    if (somiglia && !nomiVisti.Contains(collegato))
                    {
                        nome = collegato;
                        break;
                    }
                }

                // Due controller con lo stesso nome: aggiungiamo il numero
                if (nomiVisti.Contains(nome))
                {
                    nome = nome + " " + numero;
                }

                nomiVisti.Add(nome);
                lista.Add(new Dispositivo { Nome = nome, Batteria = c.Batteria, SaSeCarica = true, InCarica = c.InCarica });
            }
        }

        static void AggiungiAirPods(Dictionary<string, string> collegati, List<Dispositivo> lista, List<string> nomiVisti)
        {
            bool ciSonoAirPods = false;

            foreach (string nome in collegati.Values)
            {
                string n = nome.ToLower();
                bool eApple = n.Contains("airpods") || n.Contains("beats");
                if (!eApple || nomiVisti.Contains(nome))
                {
                    continue;
                }
                ciSonoAirPods = true;

                LetturaAirPods lettura = AirPods.Ultima();
                if (lettura != null)
                {
                    nomiVisti.Add(nome);
                    lista.Add(new Dispositivo { Nome = nome, Batteria = lettura.Auricolari, SaSeCarica = true, InCarica = lettura.InCarica });
                }
            }

            // Ascoltiamo solo quando servono (così non usa CPU per niente)
            if (ciSonoAirPods)
            {
                AirPods.Avvia();
            }
            else
            {
                AirPods.Ferma();
            }
        }

        // Trova i dispositivi Bluetooth collegati in due modi (se uno non funziona, c'è l'altro):
        // A. la funzione ufficiale di Windows "dispositivi Bluetooth collegati" (normali e Low Energy)
        // B. la proprietà "collegato" dei dispositivi Bluetooth
        static async Task<Dictionary<string, string>> TrovaCollegati()
        {
            Dictionary<string, string> collegati = new Dictionary<string, string>();

            // A. Funzione ufficiale
            string[] filtri = new string[]
            {
                BluetoothDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected),
                BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected)
            };
            foreach (string filtro in filtri)
            {
                try
                {
                    DeviceInformationCollection trovati = await DeviceInformation.FindAllAsync(filtro);
                    foreach (DeviceInformation d in trovati)
                    {
                        string indirizzo = IndirizzoAllaFine(d.Id);
                        if (indirizzo != null && !collegati.ContainsKey(indirizzo))
                        {
                            collegati[indirizzo] = d.Name;
                        }
                    }
                }
                catch (Exception)
                {
                    // proviamo con l'altro modo
                }
            }

            // B. Proprietà "collegato"
            try
            {
                DeviceInformationCollection bluetooth = await DeviceInformation.FindAllAsync(
                    FILTRO_BLUETOOTH, new string[] { CHIAVE_COLLEGATO }, DeviceInformationKind.Device);

                foreach (DeviceInformation d in bluetooth)
                {
                    Match indirizzo = Regex.Match(d.Id.ToUpper(), "DEV_([0-9A-F]{12})");
                    object stato = LeggiProprieta(d, CHIAVE_COLLEGATO);
                    if (indirizzo.Success && stato is bool && (bool)stato && !collegati.ContainsKey(indirizzo.Groups[1].Value))
                    {
                        collegati[indirizzo.Groups[1].Value] = d.Name;
                    }
                }
            }
            catch (Exception)
            {
            }

            return collegati;
        }

        // L'indirizzo (MAC) alla fine di un Id tipo "Bluetooth#Bluetooth00:1a:...-a0:b1:c2:d3:e4:f5"
        // diventa "A0B1C2D3E4F5" (null se non c'è)
        public static string IndirizzoAllaFine(string id)
        {
            Match m = Regex.Match(id.ToUpper(), "([0-9A-F]{2}:){5}[0-9A-F]{2}$");
            if (!m.Success)
            {
                return null;
            }
            return m.Value.Replace(":", "");
        }

        // Testo con tutto quello che l'app vede (da mandare per capire i problemi)
        public static async Task<string> Diagnostica()
        {
            StringBuilder testo = new StringBuilder();
            testo.AppendLine("Batterie in Barra " + Aggiornamenti.VersioneAttuale());
            testo.AppendLine("Windows: " + Environment.OSVersion.VersionString);
            testo.AppendLine();

            testo.AppendLine("== Collegati (funzione ufficiale) ==");
            string[] filtri = new string[]
            {
                BluetoothDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected),
                BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected)
            };
            foreach (string filtro in filtri)
            {
                try
                {
                    foreach (DeviceInformation d in await DeviceInformation.FindAllAsync(filtro))
                    {
                        testo.AppendLine("- " + d.Name + " | " + d.Id);
                    }
                }
                catch (Exception errore)
                {
                    testo.AppendLine("errore: " + errore.Message);
                }
            }

            testo.AppendLine();
            testo.AppendLine("== Dispositivi Bluetooth (proprietà collegato) ==");
            try
            {
                foreach (DeviceInformation d in await DeviceInformation.FindAllAsync(FILTRO_BLUETOOTH, new string[] { CHIAVE_COLLEGATO }, DeviceInformationKind.Device))
                {
                    testo.AppendLine("- " + d.Name + " | collegato=" + LeggiProprieta(d, CHIAVE_COLLEGATO) + " | " + d.Id);
                }
            }
            catch (Exception errore)
            {
                testo.AppendLine("errore: " + errore.Message);
            }

            testo.AppendLine();
            testo.AppendLine("== Dispositivi con la batteria ==");
            try
            {
                foreach (DeviceInformation d in await CercaDispositiviBluetooth())
                {
                    object valore = LeggiProprieta(d, CHIAVE_BATTERIA);
                    if (valore != null)
                    {
                        testo.AppendLine("- " + d.Name + " | batteria=" + valore + " | " + d.Id);
                    }
                }
            }
            catch (Exception errore)
            {
                testo.AppendLine("errore: " + errore.Message);
            }

            testo.AppendLine();
            testo.AppendLine("== Lettura di riserva (PowerShell) ==");
            Dictionary<string, int> riserva = await LeggiConPowerShell();
            if (riserva.Count == 0)
            {
                testo.AppendLine("(niente)");
            }
            foreach (KeyValuePair<string, int> coppia in riserva)
            {
                testo.AppendLine("- " + coppia.Key + " | batteria=" + coppia.Value);
            }

            testo.AppendLine();
            testo.AppendLine("== Controller PlayStation ==");
            List<ControllerTrovato> controller = await PlayStation.Leggi();
            if (controller.Count == 0)
            {
                testo.AppendLine("(nessuno)");
            }
            foreach (ControllerTrovato c in controller)
            {
                testo.AppendLine("- " + (c.EPS5 ? "PS5" : "PS4") + " | batteria=" + c.Batteria + " | in carica=" + c.InCarica);
            }

            testo.AppendLine();
            LetturaAirPods airpods = AirPods.Ultima();
            if (airpods != null)
            {
                testo.AppendLine("AirPods: auricolari=" + airpods.Auricolari + "% custodia=" + airpods.Custodia + "% segnale=" + airpods.Segnale);
            }
            else
            {
                testo.AppendLine("AirPods: nessun messaggio ricevuto");
            }

            StatoBatteriaPC pc = BatteriaPC.Leggi();
            testo.AppendLine("Batteria PC: " + (pc == null ? "nessuna" : pc.Percentuale + "%"));

            testo.AppendLine();
            testo.AppendLine("== Registro (ultime 40 righe) ==");
            testo.AppendLine(Registro.Ultime(40));
            return testo.ToString();
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

    // La batteria del PC (solo se è un portatile)
    public class StatoBatteriaPC
    {
        public int Percentuale;
        public bool InCarica;
        public int SecondiRimasti;   // -1 se Windows non lo sa
    }

    public static class BatteriaPC
    {
        [StructLayout(LayoutKind.Sequential)]
        struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;        // 1 = attaccato alla corrente
            public byte BatteryFlag;         // 128 = nessuna batteria, 255 = sconosciuto
            public byte BatteryLifePercent;  // 0-100 (255 = sconosciuto)
            public byte SystemStatusFlag;
            public int BatteryLifeTime;      // secondi rimasti (-1 = sconosciuto)
            public int BatteryFullLifeTime;
        }

        [DllImport("kernel32.dll")]
        static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS stato);

        // Il PC ha una batteria? (portatile sì, PC fisso no)
        public static bool CeLaBatteria()
        {
            SYSTEM_POWER_STATUS stato;
            if (!GetSystemPowerStatus(out stato))
            {
                return false;
            }
            return (stato.BatteryFlag & 128) == 0 && stato.BatteryFlag != 255 && stato.BatteryLifePercent <= 100;
        }

        // Legge la batteria del PC (null se non c'è)
        public static StatoBatteriaPC Leggi()
        {
            if (!CeLaBatteria())
            {
                return null;
            }

            SYSTEM_POWER_STATUS stato;
            GetSystemPowerStatus(out stato);

            StatoBatteriaPC risultato = new StatoBatteriaPC();
            risultato.Percentuale = stato.BatteryLifePercent;
            risultato.InCarica = stato.ACLineStatus == 1;
            risultato.SecondiRimasti = stato.BatteryLifeTime;
            return risultato;
        }
    }
}
