// TestVarie.cs
// Controlla le funzioni piccole: versioni, file della release, indirizzi Bluetooth,
// Non disturbare, colori e traduzioni.

using System.Windows.Media;
using Xunit;

namespace BatterieInBarra.Test
{
    public class TestVarie
    {
        // ----- Versioni -----

        [Fact]
        public void Riconosce_le_versioni_piu_nuove()
        {
            Assert.True(Aggiornamenti.EPiuNuova("0.2.7", "0.2.6"));
            Assert.True(Aggiornamenti.EPiuNuova("0.10.0", "0.9.9"));   // 10 è più di 9, anche se "1" < "9"
            Assert.False(Aggiornamenti.EPiuNuova("0.2.6", "0.2.6"));
            Assert.False(Aggiornamenti.EPiuNuova("0.2.5", "0.2.6"));
            Assert.False(Aggiornamenti.EPiuNuova("ciao", "0.2.6"));     // versione sbagliata = non aggiorna
        }

        [Fact]
        public void Sceglie_il_file_dell_app_e_non_l_installer()
        {
            Assert.True(Aggiornamenti.EFileApp("BatterieInBarra-0.2.7.exe"));
            Assert.False(Aggiornamenti.EFileApp("BatterieInBarra-Setup-0.2.7.exe"));
            Assert.False(Aggiornamenti.EFileApp("latest.yml"));
        }

        // ----- Indirizzi Bluetooth -----

        [Fact]
        public void Legge_l_indirizzo_Bluetooth_dall_Id()
        {
            Assert.Equal("A0B1C2D3E4F5", Batterie.IndirizzoAllaFine("Bluetooth#Bluetooth00:1a:7d:da:71:13-a0:b1:c2:d3:e4:f5"));
            Assert.Equal("D43E22AABBCC", Batterie.IndirizzoAllaFine("BluetoothLE#BluetoothLEe4:a4:71:00:11:22-d4:3e:22:aa:bb:cc"));
            Assert.Null(Batterie.IndirizzoAllaFine("BTHENUM\\DEV_A0B1C2D3E4F5\\7&1234"));
        }

        // ----- Non disturbare -----

        [Fact]
        public void Silenzio_che_passa_la_mezzanotte()
        {
            // dalle 23 alle 7
            Assert.True(Impostazioni.OraSilenziosa(23, 23, 7));
            Assert.True(Impostazioni.OraSilenziosa(3, 23, 7));
            Assert.False(Impostazioni.OraSilenziosa(7, 23, 7));
            Assert.False(Impostazioni.OraSilenziosa(12, 23, 7));
            Assert.False(Impostazioni.OraSilenziosa(22, 23, 7));
        }

        [Fact]
        public void Silenzio_che_inizia_a_mezzanotte()
        {
            // dalle 0 alle 6
            Assert.True(Impostazioni.OraSilenziosa(0, 0, 6));
            Assert.True(Impostazioni.OraSilenziosa(5, 0, 6));
            Assert.False(Impostazioni.OraSilenziosa(6, 0, 6));
            Assert.False(Impostazioni.OraSilenziosa(23, 0, 6));
        }

        // ----- Colori -----

        [Fact]
        public void Colori_delle_batterie()
        {
            Grafica.SogliaGiallo = 40;
            Grafica.SogliaRosso = 20;
            Grafica.TuttoBianco = false;

            Assert.Equal(Grafica.Rosso, Grafica.ColoreBatteria(10));
            Assert.Equal(Grafica.Rosso, Grafica.ColoreBatteria(20));
            Assert.Equal(Grafica.Giallo, Grafica.ColoreBatteria(30));
            Assert.Equal(Grafica.Verde, Grafica.ColoreBatteria(90));
        }

        [Fact]
        public void Batterie_bianche_diventano_rosse_solo_se_scariche()
        {
            Grafica.SogliaGiallo = 40;
            Grafica.SogliaRosso = 20;
            Grafica.TuttoBianco = true;
            Grafica.ColoreNeutro = Colors.White;

            Assert.Equal(Colors.White, Grafica.ColoreBatteria(30));
            Assert.Equal(Colors.White, Grafica.ColoreBatteria(90));
            Assert.Equal(Grafica.Rosso, Grafica.ColoreBatteria(15));

            Grafica.TuttoBianco = false;   // rimettiamo come prima
        }

        // ----- Grandezze (barra di avanzamento e spazio occupato) -----

        [Fact]
        public void Grandezze_dei_file()
        {
            Assert.Equal("512 byte", Testi.Grandezza(512));
            Assert.Equal("2 KB", Testi.Grandezza(2 * 1024));
            Assert.Equal("5 MB", Testi.Grandezza(5 * 1024 * 1024));
        }

        // ----- Traduzioni -----

        [Fact]
        public void Le_scritte_esistono_e_mettono_i_valori()
        {
            // Se una chiave non esiste, Testi.T restituisce la chiave stessa
            Assert.NotEqual("batterie", Testi.T("batterie"));
            Assert.NotEqual("sezione_dispositivi", Testi.T("sezione_dispositivi"));
            Assert.Contains("0.2.7", Testi.T("versione", "0.2.7"));
            Assert.Contains("85", Testi.T("avviso_scarica_testo", "Cuffie", 85));
        }

        [Fact]
        public void Percentuale_dei_controller_Xbox()
        {
            Assert.Equal(50, ControllerXbox.Percentuale(1000, 2000));
            Assert.Equal(100, ControllerXbox.Percentuale(2100, 2000));   // mai sopra 100
            Assert.Equal(-1, ControllerXbox.Percentuale(500, 0));        // numeri senza senso
        }

        [Fact]
        public void Riconosce_AirPods_e_Beats_dal_nome()
        {
            Assert.True(Batterie.EApple("AirPods Pro di Luca"));
            Assert.True(Batterie.EApple("Le AirPod di Marco"));
            Assert.True(Batterie.EApple("Beats Studio Buds"));
            Assert.False(Batterie.EApple("WH-1000XM5"));
        }

        [Fact]
        public void Icona_giusta_per_auricolari_e_cuffie()
        {
            Assert.Equal("auricolari", Grafica.TipoDispositivo("AirPods Pro"));
            Assert.Equal("auricolari", Grafica.TipoDispositivo("Galaxy Buds2"));
            Assert.Equal("cuffie", Grafica.TipoDispositivo("WH-1000XM5"));
            Assert.Equal("controller", Grafica.TipoDispositivo("Xbox Wireless Controller"));
        }

        [Fact]
        public void Icone_di_base_e_scelta()
        {
            Impostazioni opzioni = new Impostazioni();
            // Di base: cuffie piene (2), gli altri a linee (1)
            Assert.Equal(2, opzioni.ModelloDi("cuffie"));
            Assert.Equal(1, opzioni.ModelloDi("mouse"));

            opzioni.ModelliIcone["mouse"] = 3;
            Assert.Equal(3, opzioni.ModelloDi("mouse"));

            // Un numero sbagliato torna a quello di base
            opzioni.ModelliIcone["tastiera"] = 7;
            Assert.Equal(1, opzioni.ModelloDi("tastiera"));

            // Un'immagine caricata che non esiste più viene ignorata
            opzioni.IconePersonali["cuffie"] = "C:\\non\\esiste.png";
            Assert.Null(opzioni.IconaPersonaleDi("cuffie"));
        }

        [Fact]
        public void Si_puo_scegliere_la_lingua()
        {
            Testi.ImpostaLingua("de");
            Assert.Equal("Akkus", Testi.T("batterie"));
            Testi.ImpostaLingua("it");
            Assert.Equal("Batterie", Testi.T("batterie"));
            Testi.ImpostaLingua("en");
            Assert.Equal("Batteries", Testi.T("batterie"));
            Testi.ImpostaLingua("auto");   // torniamo a quella del PC
        }

        [Fact]
        public void Ogni_tipo_ha_3_disegni_diversi()
        {
            foreach (string tipo in Grafica.TIPI_PERSONALIZZABILI)
            {
                string uno = string.Join("|", Grafica.Disegno(tipo, 1));
                string due = string.Join("|", Grafica.Disegno(tipo, 2));
                string tre = string.Join("|", Grafica.Disegno(tipo, 3));
                Assert.NotEqual(uno, due);
                Assert.NotEqual(due, tre);
                Assert.NotEqual(uno, tre);
            }
        }
    }
}
