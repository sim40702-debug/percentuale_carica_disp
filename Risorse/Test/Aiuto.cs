// Aiuto.cs
// Cose che servono a tutti i test.

using System;
using System.IO;
using Xunit;

// I test usano cose condivise (cartella dei dati, colori): li facciamo andare uno alla volta
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace BatterieInBarra.Test
{
    public static class Aiuto
    {
        // Una cartella vuota nuova per ogni test, così i dati veri non vengono toccati
        public static void CartellaNuova()
        {
            string cartella = Path.Combine(Path.GetTempPath(), "BatterieInBarraTest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(cartella);
            Impostazioni.CartellaDiProva = cartella;
        }

        // Trasforma un testo tipo "0719010e..." in byte (per i messaggi delle AirPods)
        public static byte[] Byte(string esadecimale)
        {
            return Convert.FromHexString(esadecimale);
        }
    }
}
