// Finestre.cs
// Funzioni di Windows per:
// - sapere dove si trova la barra e dove iniziano le icone vicino all'orologio
// - capire se c'è un gioco o un video a schermo intero
// - mettere la nostra finestrella sopra la barra senza "rubare" il clic

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace BatterieInBarra
{
    // Le informazioni sulla barra di Windows (in pixel "veri" dello schermo)
    public class InfoBarra
    {
        public int Alto;            // dove inizia la barra (in alto)
        public int Altezza;         // quanto è alta
        public int InizioIcone;     // dove iniziano le icone vicino all'orologio
        public double Scala;        // zoom dello schermo (1 = 100%, 1.5 = 150%...)
        public bool SchermoIntero;  // c'è un gioco o un video a schermo intero?
    }

    public static class Finestre
    {
        // ----- Strutture e funzioni di Windows (user32.dll e dwmapi.dll) -----

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindow(string classe, string titolo);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindowEx(IntPtr padre, IntPtr dopo, string classe, string titolo);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr finestra, out RECT rettangolo);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetClassName(IntPtr finestra, StringBuilder testo, int massimo);

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr finestra, uint opzioni);

        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);

        [DllImport("user32.dll")]
        static extern uint GetDpiForWindow(IntPtr finestra);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr finestra, IntPtr dopo, int x, int y, int larghezza, int altezza, uint opzioni);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr finestra, int indice);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr finestra, int indice, int valore);

        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr finestra, int numero, uint tasti, uint tasto);

        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr finestra, int numero);

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr finestra, int attributo, ref int valore, int grandezza);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOACTIVATE = 0x0010;
        const uint MONITOR_DEFAULTTONEAREST = 2;
        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        const int WS_EX_NOACTIVATE = 0x08000000;
        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;


        // ----- La barra di Windows -----

        public static InfoBarra LeggiBarraWindows()
        {
            IntPtr barra = FindWindow("Shell_TrayWnd", null);
            if (barra == IntPtr.Zero)
            {
                return null;
            }

            RECT r;
            GetWindowRect(barra, out r);

            InfoBarra info = new InfoBarra();
            info.Alto = r.Top;
            info.Altezza = r.Bottom - r.Top;
            info.Scala = GetDpiForWindow(barra) / 96.0;
            if (info.Scala <= 0)
            {
                info.Scala = 1;
            }

            // Se non troviamo le icone, facciamo una stima: 260 pixel prima del bordo destro
            info.InizioIcone = r.Right - (int)(260 * info.Scala);

            IntPtr icone = FindWindowEx(barra, IntPtr.Zero, "TrayNotifyWnd", null);
            if (icone != IntPtr.Zero)
            {
                RECT ri;
                GetWindowRect(icone, out ri);
                if (ri.Left > r.Left)
                {
                    info.InizioIcone = ri.Left;
                }
            }

            info.SchermoIntero = CeQualcosaASchermoIntero();
            return info;
        }

        // C'è una finestra a schermo intero davanti? (gioco, video...)
        static bool CeQualcosaASchermoIntero()
        {
            IntPtr davanti = GetForegroundWindow();
            if (davanti == IntPtr.Zero)
            {
                return false;
            }

            // Il desktop e la barra non contano
            StringBuilder classe = new StringBuilder(256);
            GetClassName(davanti, classe, 256);
            string nome = classe.ToString();
            if (nome == "Progman" || nome == "WorkerW" || nome == "Shell_TrayWnd")
            {
                return false;
            }

            RECT rd;
            GetWindowRect(davanti, out rd);

            MONITORINFO schermo = new MONITORINFO();
            schermo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            GetMonitorInfo(MonitorFromWindow(davanti, MONITOR_DEFAULTTONEAREST), ref schermo);

            return rd.Left <= schermo.rcMonitor.Left
                && rd.Top <= schermo.rcMonitor.Top
                && rd.Right >= schermo.rcMonitor.Right
                && rd.Bottom >= schermo.rcMonitor.Bottom;
        }


        // ----- Le nostre finestre -----

        static IntPtr Maniglia(Window finestra)
        {
            return new WindowInteropHelper(finestra).EnsureHandle();
        }

        // La finestra non "ruba" il clic e non compare in Alt+Tab
        public static void RendiNonAttivabile(Window finestra)
        {
            IntPtr h = Maniglia(finestra);
            int stile = GetWindowLong(h, GWL_EXSTYLE);
            SetWindowLong(h, GWL_EXSTYLE, stile | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
        }

        // Mette la finestra in quel punto (pixel veri) e davanti a tutto, anche alla barra
        public static void MettiDavanti(Window finestra, int x, int y, int larghezza, int altezza)
        {
            SetWindowPos(Maniglia(finestra), HWND_TOPMOST, x, y, larghezza, altezza, SWP_NOACTIVATE);
        }

        // Dove si trova la finestra (pixel veri)
        public static RECT Posizione(Window finestra)
        {
            RECT r;
            GetWindowRect(Maniglia(finestra), out r);
            return r;
        }

        // Zoom dello schermo dove si trova la finestra
        public static double Scala(Window finestra)
        {
            double scala = GetDpiForWindow(Maniglia(finestra)) / 96.0;
            if (scala <= 0)
            {
                scala = 1;
            }
            return scala;
        }

        // ----- Scorciatoia da tastiera (Ctrl + Alt + B) -----

        public const int NUMERO_SCORCIATOIA = 1;
        public const int WM_HOTKEY = 0x0312;
        const uint MOD_ALT = 0x0001;
        const uint MOD_CONTROL = 0x0002;
        const uint MOD_NOREPEAT = 0x4000;
        const uint TASTO_B = 0x42;

        // Chiede a Windows di avvisare questa finestra quando premi Ctrl + Alt + B
        public static bool AttivaScorciatoia(Window finestra)
        {
            return RegisterHotKey(Maniglia(finestra), NUMERO_SCORCIATOIA, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, TASTO_B);
        }

        public static void TogliScorciatoia(Window finestra)
        {
            UnregisterHotKey(Maniglia(finestra), NUMERO_SCORCIATOIA);
        }

        // Barra del titolo scura (per la finestra Impostazioni)
        public static void TitoloScuro(Window finestra, bool scuro)
        {
            int valore = scuro ? 1 : 0;
            DwmSetWindowAttribute(Maniglia(finestra), DWMWA_USE_IMMERSIVE_DARK_MODE, ref valore, sizeof(int));
        }
    }
}
