// TestPlayStation.cs
// Controlla che la batteria dei controller PlayStation venga letta dal punto giusto dei dati.

using Xunit;

namespace BatterieInBarra.Test
{
    public class TestPlayStation
    {
        // Crea un rapporto finto: "tipo" all'inizio e "valore" nel byte della batteria
        byte[] Rapporto(byte tipo, int lunghezza, int posizione, byte valore)
        {
            byte[] r = new byte[lunghezza];
            r[0] = tipo;
            r[posizione] = valore;
            return r;
        }

        [Fact]
        public void PS4_col_cavo_in_carica()
        {
            // cavo (0x10) e livello 11 su 11 -> 100%
            Assert.Equal(100, PlayStation.LeggiBatteria(Rapporto(0x01, 64, 30, 0x1B), false));
        }

        [Fact]
        public void PS4_via_Bluetooth()
        {
            // senza cavo, livello 4 su 8 -> 50%
            Assert.Equal(50, PlayStation.LeggiBatteria(Rapporto(0x11, 547, 32, 0x04), false));
        }

        [Fact]
        public void PS4_rapporto_corto_via_Bluetooth_viene_ignorato()
        {
            // via Bluetooth il primo rapporto 0x01 non ha la batteria (sta in un buffer da 547 byte)
            Assert.Equal(-1, PlayStation.LeggiBatteria(Rapporto(0x01, 547, 30, 0x04), false));
        }

        [Fact]
        public void PS4_capisce_se_e_in_carica()
        {
            // col cavo e livello 6 su 11: in carica
            Assert.True(PlayStation.LeggiInCarica(Rapporto(0x01, 64, 30, 0x16), false));
            // col cavo ma pieno (11): non sta più caricando
            Assert.False(PlayStation.LeggiInCarica(Rapporto(0x01, 64, 30, 0x1B), false));
            // senza cavo: no
            Assert.False(PlayStation.LeggiInCarica(Rapporto(0x11, 547, 32, 0x04), false));
        }

        [Fact]
        public void PS5_capisce_se_e_in_carica()
        {
            // cifra alta 1 = in carica, 2 = carica completa, 0 = a batteria
            Assert.True(PlayStation.LeggiInCarica(Rapporto(0x31, 78, 54, 0x16), true));
            Assert.False(PlayStation.LeggiInCarica(Rapporto(0x01, 64, 53, 0x2A), true));
            Assert.False(PlayStation.LeggiInCarica(Rapporto(0x31, 78, 54, 0x06), true));
        }

        [Fact]
        public void PS5_via_Bluetooth_e_col_cavo()
        {
            // livello 6 -> 65%
            Assert.Equal(65, PlayStation.LeggiBatteria(Rapporto(0x31, 78, 54, 0x06), true));
            // col cavo, carica completa (stato 2) -> 100%
            Assert.Equal(100, PlayStation.LeggiBatteria(Rapporto(0x01, 64, 53, 0x2A), true));
        }
    }
}
