// TestAirPods.cs
// Controlla che i messaggi Bluetooth delle AirPods vengano letti bene.

using Xunit;

namespace BatterieInBarra.Test
{
    public class TestAirPods
    {
        [Fact]
        public void AirPodsPro_al_90_con_custodia_chiusa()
        {
            LetturaAirPods lettura = AirPods.LeggiMessaggio(Aiuto.Byte("0719010e202b998f01000500000000000000000000000000000000"), -50);

            Assert.NotNull(lettura);
            Assert.Equal(90, lettura.Auricolari);
            Assert.Equal(-1, lettura.Custodia);   // custodia chiusa = sconosciuta
        }

        [Fact]
        public void Prende_l_auricolare_piu_scarico_e_legge_la_custodia()
        {
            // sinistro 100%, destro 70%, custodia 50%
            LetturaAirPods lettura = AirPods.LeggiMessaggio(Aiuto.Byte("07190102202ba7350100050000000000000000000000000000000000"), -50);

            Assert.NotNull(lettura);
            Assert.Equal(70, lettura.Auricolari);
            Assert.Equal(50, lettura.Custodia);
        }

        [Fact]
        public void Auricolari_nella_custodia_vengono_ignorati()
        {
            LetturaAirPods lettura = AirPods.LeggiMessaggio(Aiuto.Byte("071901022055ff0501000500000000000000000000000000000000"), -50);

            Assert.Null(lettura);
        }

        [Fact]
        public void Messaggi_Apple_che_non_sono_AirPods_vengono_ignorati()
        {
            LetturaAirPods lettura = AirPods.LeggiMessaggio(Aiuto.Byte("1005031c1e7a2b"), -50);

            Assert.Null(lettura);
        }
    }
}
