// TestCalcoli.cs
// Controlla i calcoli con la cronologia: tempo stimato, durata di una carica,
// cariche del mese e salute della batteria.

using System;
using Xunit;

namespace BatterieInBarra.Test
{
    public class TestCalcoli
    {
        // Mentre un dispositivo è collegato, l'app salva una lettura almeno ogni 10 minuti.
        // Nei test usiamo 12 minuti (sotto i 20 minuti che l'app considera "scollegato").
        const long DODICI_MINUTI = 12 * 60 * 1000;
        const long UN_ORA = 60 * 60 * 1000;

        // Una cuffia che scende dell'1% ogni 12 minuti: da 100% a 80% in 4 ore
        Cronologia CuffiaCheSiScarica()
        {
            Aiuto.CartellaNuova();
            Cronologia cronologia = new Cronologia();

            long inizio = Cronologia.Adesso() - 11 * UN_ORA;
            for (int i = 0; i <= 20; i++)
            {
                cronologia.AggiungiLetturaIn("Cuffie", 100 - i, inizio + i * DODICI_MINUTI);
            }
            return cronologia;
        }

        [Fact]
        public void Tempo_stimato()
        {
            Cronologia cronologia = CuffiaCheSiScarica();

            // 1% ogni 12 minuti e restano 80% -> 80 x 12 minuti = 16 ore
            Assert.Equal(Testi.T("circa_ore", 16), cronologia.TempoStimato("Cuffie"));
        }

        [Fact]
        public void Durata_di_una_carica_completa()
        {
            Cronologia cronologia = CuffiaCheSiScarica();

            // 1% ogni 12 minuti -> 100% in 20 ore
            Assert.Equal(20, cronologia.OrePerCaricaCompleta("Cuffie"), 1);
        }

        [Fact]
        public void Con_pochi_dati_dice_calcolo_in_corso()
        {
            Aiuto.CartellaNuova();
            Cronologia cronologia = new Cronologia();
            cronologia.AggiungiLetturaIn("Mouse", 80, Cronologia.Adesso() - UN_ORA);
            cronologia.AggiungiLetturaIn("Mouse", 79, Cronologia.Adesso());

            Assert.Equal(Testi.T("calcolo"), cronologia.TempoStimato("Mouse"));
            Assert.Equal(-1, cronologia.OrePerCaricaCompleta("Mouse"));
        }

        [Fact]
        public void Le_pause_da_scollegato_non_contano()
        {
            Aiuto.CartellaNuova();
            Cronologia cronologia = new Cronologia();
            long inizio = Cronologia.Adesso() - 20 * UN_ORA;

            // 2 ore di uso (da 100 a 90), poi 8 ore spenta, poi altre 2 ore (da 90 a 80)
            for (int i = 0; i <= 10; i++)
            {
                cronologia.AggiungiLetturaIn("Tastiera", 100 - i, inizio + i * DODICI_MINUTI);
            }
            long dopoLaPausa = inizio + 10 * DODICI_MINUTI + 8 * UN_ORA;
            for (int i = 0; i <= 10; i++)
            {
                cronologia.AggiungiLetturaIn("Tastiera", 90 - i, dopoLaPausa + i * DODICI_MINUTI);
            }

            // Uso vero: 4 ore per 20% -> 100% = 20 ore (le 8 ore spenta non contano)
            Assert.Equal(20, cronologia.OrePerCaricaCompleta("Tastiera"), 1);
        }

        [Fact]
        public void Una_carica_fa_ripartire_il_calcolo()
        {
            Cronologia cronologia = CuffiaCheSiScarica();

            // La carichi al 100%: da lì si riparte, quindi i dati sono troppo pochi
            cronologia.AggiungiLetturaIn("Cuffie", 100, Cronologia.Adesso());
            Assert.Equal(Testi.T("calcolo"), cronologia.TempoStimato("Cuffie"));
        }

        [Fact]
        public void Statistiche_delle_cariche()
        {
            Aiuto.CartellaNuova();
            Statistiche statistiche = new Statistiche();
            statistiche.RegistraCarica("Mouse", 20);
            statistiche.RegistraCarica("Mouse", 30);
            statistiche.RegistraCarica("Mouse", -1);   // durata sconosciuta: conta come carica ma non nella media

            Assert.Equal(3, statistiche.CaricheQuestoMese("Mouse"));
            Assert.Equal(25, statistiche.DurataMedia("Mouse"), 1);
            Assert.Equal(-1, statistiche.DurataMedia("Controller"));   // mai caricato
        }

        [Fact]
        public void Batteria_peggiorata()
        {
            Aiuto.CartellaNuova();
            Statistiche statistiche = new Statistiche();

            // Prima durava 20 ore, le ultime due volte solo 10: meno del 70% -> peggiorata
            statistiche.RegistraCarica("Cuffie", 20);
            statistiche.RegistraCarica("Cuffie", 20);
            statistiche.RegistraCarica("Cuffie", 20);
            statistiche.RegistraCarica("Cuffie", 10);
            statistiche.RegistraCarica("Cuffie", 10);

            Assert.True(statistiche.BatteriaPeggiorata("Cuffie"));
        }

        [Fact]
        public void Batteria_normale_non_e_peggiorata()
        {
            Aiuto.CartellaNuova();
            Statistiche statistiche = new Statistiche();

            statistiche.RegistraCarica("Cuffie", 20);
            statistiche.RegistraCarica("Cuffie", 20);
            statistiche.RegistraCarica("Cuffie", 20);
            statistiche.RegistraCarica("Cuffie", 19);
            statistiche.RegistraCarica("Cuffie", 18);

            Assert.False(statistiche.BatteriaPeggiorata("Cuffie"));
        }

        [Fact]
        public void Le_impostazioni_si_salvano_e_si_rileggono()
        {
            Aiuto.CartellaNuova();
            Impostazioni opzioni = new Impostazioni();
            opzioni.Nomi["WH-1000XM5"] = "Cuffie";
            opzioni.Soglia = 30;
            opzioni.Salva();

            Impostazioni rilette = Impostazioni.Leggi();
            Assert.Equal("Cuffie", rilette.NomeDi("WH-1000XM5"));
            Assert.Equal("MX Keys", rilette.NomeDi("MX Keys"));   // senza nome scelto resta l'originale
            Assert.Equal(30, rilette.Soglia);
        }
    }
}
