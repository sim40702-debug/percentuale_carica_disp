// PlayStation.cs
// Windows non conosce la batteria dei controller PlayStation (PS4 DualShock 4 e PS5 DualSense):
// il controller la manda dentro i suoi "rapporti" (i dati dei tasti), che leggiamo direttamente
// come fanno DS4Windows e Steam.
//
// Dove sta la batteria:
// - PS4 via cavo (rapporto 0x01): byte 30     - PS4 via Bluetooth (rapporto 0x11): byte 32
//   cifra bassa = livello, bit 0x10 = attaccato al cavo (livello 0-11 col cavo, 0-8 senza)
// - PS5 via cavo (rapporto 0x01): byte 53     - PS5 via Bluetooth (rapporto 0x31): byte 54
//   cifra bassa = livello 0-10, cifra alta = 0 a batteria, 1 in carica, 2 carica completa

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;

namespace BatterieInBarra
{
    public class ControllerTrovato
    {
        public bool EPS5;        // true = DualSense (PS5), false = DualShock 4 (PS4)
        public int Batteria;     // percentuale
    }

    public static class PlayStation
    {
        const ushort SONY = 0x054C;
        const ushort PAGINA_COMANDI = 0x01;   // "Generic Desktop"
        const ushort GAMEPAD = 0x05;

        // I modelli: DualShock 4 (prima e seconda versione, chiavetta Sony) e DualSense (normale ed Edge)
        static readonly ushort[] MODELLI_PS4 = { 0x05C4, 0x09CC, 0x0BA0 };
        static readonly ushort[] MODELLI_PS5 = { 0x0CE6, 0x0DF2 };

        // ----- Funzioni di Windows per leggere i dati del controller -----

        [StructLayout(LayoutKind.Sequential)]
        struct HIDP_CAPS
        {
            public ushort Usage;
            public ushort UsagePage;
            public ushort InputReportByteLength;
            public ushort OutputReportByteLength;
            public ushort FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public ushort[] Reserved;
            public ushort NumberLinkCollectionNodes;
            public ushort NumberInputButtonCaps;
            public ushort NumberInputValueCaps;
            public ushort NumberInputDataIndices;
            public ushort NumberOutputButtonCaps;
            public ushort NumberOutputValueCaps;
            public ushort NumberOutputDataIndices;
            public ushort NumberFeatureButtonCaps;
            public ushort NumberFeatureValueCaps;
            public ushort NumberFeatureDataIndices;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern SafeFileHandle CreateFile(string percorso, uint accesso, uint condivisione, IntPtr sicurezza,
            uint creazione, uint opzioni, IntPtr modello);

        [DllImport("hid.dll")]
        static extern bool HidD_GetPreparsedData(SafeFileHandle dispositivo, out IntPtr dati);

        [DllImport("hid.dll")]
        static extern bool HidD_FreePreparsedData(IntPtr dati);

        [DllImport("hid.dll")]
        static extern int HidP_GetCaps(IntPtr dati, out HIDP_CAPS caratteristiche);

        [DllImport("hid.dll")]
        static extern bool HidD_GetFeature(SafeFileHandle dispositivo, byte[] buffer, int lunghezza);

        const uint GENERIC_READ = 0x80000000;
        const uint GENERIC_WRITE = 0x40000000;
        const uint FILE_SHARE_READ = 0x1;
        const uint FILE_SHARE_WRITE = 0x2;
        const uint OPEN_EXISTING = 3;
        const uint FILE_FLAG_OVERLAPPED = 0x40000000;


        // ----- Lettura -----

        // Cerca i controller PlayStation collegati e legge la loro batteria
        public static async Task<List<ControllerTrovato>> Leggi()
        {
            List<ControllerTrovato> trovati = new List<ControllerTrovato>();

            foreach (ushort modello in MODELLI_PS4)
            {
                await CercaModello(modello, false, trovati);
            }
            foreach (ushort modello in MODELLI_PS5)
            {
                await CercaModello(modello, true, trovati);
            }
            return trovati;
        }

        static async Task CercaModello(ushort modello, bool ePS5, List<ControllerTrovato> trovati)
        {
            try
            {
                string filtro = HidDevice.GetDeviceSelector(PAGINA_COMANDI, GAMEPAD, SONY, modello);
                foreach (DeviceInformation d in await DeviceInformation.FindAllAsync(filtro))
                {
                    int batteria = await LeggiController(d.Id, ePS5);
                    if (batteria >= 0)
                    {
                        trovati.Add(new ControllerTrovato { EPS5 = ePS5, Batteria = batteria });
                    }
                }
            }
            catch (Exception errore)
            {
                Registro.Scrivi("PlayStation: errore nella ricerca: " + errore.Message);
            }
        }

        // Apre il controller, legge qualche rapporto e trova la batteria (-1 se non riesce)
        static async Task<int> LeggiController(string percorso, bool ePS5)
        {
            SafeFileHandle maniglia = CreateFile(percorso, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, IntPtr.Zero);
            if (maniglia.IsInvalid)
            {
                Registro.Scrivi("PlayStation: non riesco ad aprire il controller (errore " + Marshal.GetLastWin32Error() + ")");
                return -1;
            }

            try
            {
                // Quanto è lungo un rapporto?
                IntPtr dati;
                HIDP_CAPS caratteristiche;
                if (!HidD_GetPreparsedData(maniglia, out dati))
                {
                    return -1;
                }
                HidP_GetCaps(dati, out caratteristiche);
                HidD_FreePreparsedData(dati);

                // Via Bluetooth il controller manda i dati completi solo dopo che gli chiediamo
                // il rapporto 0x05 (come fanno DS4Windows e Steam)
                if (caratteristiche.FeatureReportByteLength > 0)
                {
                    byte[] richiesta = new byte[caratteristiche.FeatureReportByteLength];
                    richiesta[0] = 0x05;
                    HidD_GetFeature(maniglia, richiesta, richiesta.Length);
                }

                using (FileStream flusso = new FileStream(maniglia, FileAccess.Read, caratteristiche.InputReportByteLength, true))
                {
                    // Leggiamo fino a 10 rapporti: quello giusto arriva in pochi millisecondi
                    byte ultimoTipo = 0;
                    for (int i = 0; i < 10; i++)
                    {
                        byte[] rapporto = new byte[caratteristiche.InputReportByteLength];
                        Task<int> lettura = flusso.ReadAsync(rapporto, 0, rapporto.Length);
                        if (await Task.WhenAny(lettura, Task.Delay(1000)) != lettura)
                        {
                            Registro.Scrivi("PlayStation: il controller non manda dati (è acceso?)");
                            return -1;
                        }

                        ultimoTipo = rapporto[0];
                        int batteria = LeggiBatteria(rapporto, ePS5);
                        if (batteria >= 0)
                        {
                            return batteria;
                        }
                    }
                    Registro.Scrivi("PlayStation: nessun rapporto con la batteria (ultimo tipo 0x"
                        + ultimoTipo.ToString("X2") + ", lunghezza " + caratteristiche.InputReportByteLength + ")");
                }
                return -1;
            }
            catch (Exception errore)
            {
                Registro.Scrivi("PlayStation: errore nella lettura: " + errore.Message);
                return -1;
            }
            finally
            {
                maniglia.Dispose();
            }
        }

        // Trova la batteria dentro un rapporto. Restituisce -1 se il rapporto non la contiene.
        // (È "public" per poterla controllare con i test automatici)
        public static int LeggiBatteria(byte[] rapporto, bool ePS5)
        {
            if (rapporto.Length < 2)
            {
                return -1;
            }
            byte tipo = rapporto[0];

            if (!ePS5)
            {
                // PS4: 0x01 col cavo (rapporto di esattamente 64 byte), 0x11 via Bluetooth.
                // Attenzione: via Bluetooth arriva anche un 0x01 "corto" senza batteria (in un buffer più lungo)
                int posizione = -1;
                if (tipo == 0x01 && rapporto.Length == 64)
                {
                    posizione = 30;
                }
                if (tipo == 0x11 && rapporto.Length >= 78)
                {
                    posizione = 32;
                }
                if (posizione < 0)
                {
                    return -1;
                }

                byte valore = rapporto[posizione];
                bool cavo = (valore & 0x10) != 0;
                int livello = valore & 0x0F;
                int massimo = cavo ? 11 : 8;
                return Math.Min(livello * 100 / massimo, 100);
            }
            else
            {
                // PS5: 0x01 col cavo (esattamente 64 byte), 0x31 via Bluetooth
                int posizione = -1;
                if (tipo == 0x01 && rapporto.Length == 64)
                {
                    posizione = 53;
                }
                if (tipo == 0x31 && rapporto.Length >= 78)
                {
                    posizione = 54;
                }
                if (posizione < 0)
                {
                    return -1;
                }

                byte valore = rapporto[posizione];
                int livello = valore & 0x0F;
                int stato = valore >> 4;
                if (stato == 2)
                {
                    return 100;   // carica completa
                }
                return Math.Min(livello * 10 + 5, 100);
            }
        }
    }
}
