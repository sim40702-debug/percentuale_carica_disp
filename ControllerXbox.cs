// ControllerXbox.cs
// I controller Xbox (e altri controller che Windows conosce come "controller di gioco")
// dicono a Windows quanta batteria hanno e se sono in carica.
// Li leggiamo con le funzioni di Windows per i giochi (Windows.Gaming.Input).
// I controller PlayStation li saltiamo: hanno il loro file (PlayStation.cs).

using System;
using System.Collections.Generic;
using Windows.Devices.Power;
using Windows.Gaming.Input;
using Windows.System.Power;

namespace BatterieInBarra
{
    public static class ControllerXbox
    {
        const ushort SONY = 0x054C;   // i controller PlayStation
        static bool pronto = false;

        // Windows riempie la lista dei controller solo dopo che qualcuno "ascolta": lo facciamo all'avvio
        public static void Prepara()
        {
            if (pronto)
            {
                return;
            }
            pronto = true;
            try
            {
                RawGameController.RawGameControllerAdded += QuandoArrivaController;
            }
            catch (Exception errore)
            {
                Registro.Scrivi("Controller di gioco non disponibili: " + errore.Message);
            }
        }

        static void QuandoArrivaController(object chi, RawGameController controller)
        {
            Registro.Scrivi("Controller di gioco trovato: " + controller.DisplayName);
        }

        public static List<Dispositivo> Leggi()
        {
            Prepara();
            List<Dispositivo> trovati = new List<Dispositivo>();

            try
            {
                foreach (RawGameController controller in RawGameController.RawGameControllers)
                {
                    if (controller.HardwareVendorId == SONY)
                    {
                        continue;
                    }

                    BatteryReport rapporto = controller.TryGetBatteryReport();
                    if (rapporto == null || rapporto.Status == BatteryStatus.NotPresent)
                    {
                        continue;   // col cavo senza batteria, o non lo dice
                    }
                    if (rapporto.RemainingCapacityInMilliwattHours == null || rapporto.FullChargeCapacityInMilliwattHours == null)
                    {
                        continue;
                    }

                    int batteria = Percentuale(rapporto.RemainingCapacityInMilliwattHours.Value, rapporto.FullChargeCapacityInMilliwattHours.Value);
                    if (batteria < 0)
                    {
                        continue;
                    }

                    string nome = controller.DisplayName;
                    if (nome == null || nome.Trim() == "")
                    {
                        nome = "Controller";
                    }

                    Dispositivo d = new Dispositivo();
                    d.Nome = nome;
                    d.Batteria = batteria;
                    d.SaSeCarica = true;
                    d.InCarica = rapporto.Status == BatteryStatus.Charging;
                    trovati.Add(d);
                }
            }
            catch (Exception errore)
            {
                Registro.Scrivi("Lettura controller di gioco non riuscita: " + errore.Message);
            }
            return trovati;
        }

        // Quanta carica è rimasta, in percentuale (-1 se i numeri non hanno senso).
        // (È "public" per poterla controllare con i test automatici)
        public static int Percentuale(int rimasta, int piena)
        {
            if (piena <= 0 || rimasta < 0)
            {
                return -1;
            }
            int percentuale = (int)Math.Round(rimasta * 100.0 / piena);
            return Math.Min(percentuale, 100);
        }
    }
}
