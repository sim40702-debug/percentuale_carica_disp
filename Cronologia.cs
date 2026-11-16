// Cronologia.cs
// Si ricorda la batteria nel tempo (fino a 7 giorni), per:
// - i grafici delle ultime 24 ore e degli ultimi 7 giorni
// - il tempo stimato ("circa 6 ore rimaste")
// - la durata di una carica (per le statistiche)

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BatterieInBarra
{
    // Una lettura: quando (T, in millisecondi) e quanto (P, percentuale)
    public class Lettura
    {
        public long T { get; set; }
        public int P { get; set; }
    }

    public class Cronologia
    {
        const long UN_MINUTO = 60 * 1000;
        public const long UN_ORA = 60 * UN_MINUTO;
        const long SETTE_GIORNI = 7 * 24 * UN_ORA;
        const long OGNI_QUANTO_SALVARE = 10 * UN_MINUTO;  // un punto ogni 10 minuti (o se cambia la %)
        public const long PAUSA_MASSIMA = 20 * UN_MINUTO; // più di 20 minuti senza dati = scollegato

        // nome originale -> letture
        Dictionary<string, List<Lettura>> dati = new Dictionary<string, List<Lettura>>();
        string file;

        public Cronologia()
        {
            file = Path.Combine(Impostazioni.Cartella(), "cronologia.json");
            try
            {
                dati = JsonSerializer.Deserialize<Dictionary<string, List<Lettura>>>(File.ReadAllText(file));
            }
            catch (Exception)
            {
                // il file non c'è ancora
            }
            if (dati == null)
            {
                dati = new Dictionary<string, List<Lettura>>();
            }
        }

        public static long Adesso()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        // Aggiunge una lettura della batteria
        public void AggiungiLettura(string nome, int percentuale)
        {
            long adesso = Adesso();

            if (!dati.ContainsKey(nome))
            {
                dati[nome] = new List<Lettura>();
            }
            List<Lettura> punti = dati[nome];

            // Salviamo solo se la % è cambiata o se sono passati 10 minuti
            if (punti.Count > 0)
            {
                Lettura ultima = punti[punti.Count - 1];
                if (ultima.P == percentuale && adesso - ultima.T < OGNI_QUANTO_SALVARE)
                {
                    return;
                }
            }

            punti.Add(new Lettura { T = adesso, P = percentuale });

            // Togliamo i punti più vecchi di 7 giorni
            while (punti.Count > 0 && adesso - punti[0].T > SETTE_GIORNI)
            {
                punti.RemoveAt(0);
            }

            File.WriteAllText(file, JsonSerializer.Serialize(dati));
        }

        // L'ultima lettura salvata (null se non ce ne sono)
        public Lettura UltimaLettura(string nome)
        {
            if (!dati.ContainsKey(nome) || dati[nome].Count == 0)
            {
                return null;
            }
            return dati[nome][dati[nome].Count - 1];
        }

        // Quante letture sono salvate in tutto
        public int NumeroLetture()
        {
            int totale = 0;
            foreach (List<Lettura> punti in dati.Values)
            {
                totale = totale + punti.Count;
            }
            return totale;
        }

        // Cancella tutta la cronologia
        public void Cancella()
        {
            dati.Clear();
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }

        // I punti degli ultimi "ore" (24 per il grafico di un giorno, 168 per 7 giorni)
        public List<Lettura> UltimeOre(string nome, int ore)
        {
            List<Lettura> risultato = new List<Lettura>();
            if (!dati.ContainsKey(nome))
            {
                return risultato;
            }

            long inizio = Adesso() - ore * UN_ORA;
            foreach (Lettura l in dati[nome])
            {
                if (l.T >= inizio)
                {
                    risultato.Add(l);
                }
            }
            return risultato;
        }


        // ----- Calcoli sulla scarica -----

        // Guarda l'ultima scarica (da quando la batteria è salita l'ultima volta):
        // - tempoCollegato: quanto tempo è stato acceso (senza le pause)
        // - scesa: di quanti punti % è scesa
        // Restituisce false se non ci sono abbastanza dati.
        bool CalcolaScarica(string nome, out long tempoCollegato, out int scesa)
        {
            tempoCollegato = 0;
            scesa = 0;
            if (!dati.ContainsKey(nome) || dati[nome].Count < 2)
            {
                return false;
            }
            List<Lettura> punti = dati[nome];

            // 1. L'ultima volta che la batteria è salita (= è stata caricata)
            int inizio = 0;
            for (int i = 1; i < punti.Count; i++)
            {
                if (punti[i].P > punti[i - 1].P)
                {
                    inizio = i;
                }
            }

            // 2. Quanto tempo è stato collegato da allora
            for (int i = inizio + 1; i < punti.Count; i++)
            {
                long pausa = punti[i].T - punti[i - 1].T;
                if (pausa < PAUSA_MASSIMA)
                {
                    tempoCollegato = tempoCollegato + pausa;
                }
            }

            // 3. Quanto è scesa
            scesa = punti[inizio].P - punti[punti.Count - 1].P;
            return true;
        }

        // Quanto dura una carica completa (0-100%), guardando l'ultima scarica. -1 se non si sa.
        public double OrePerCaricaCompleta(string nome)
        {
            long tempoCollegato;
            int scesa;
            if (!CalcolaScarica(nome, out tempoCollegato, out scesa))
            {
                return -1;
            }

            // Servono almeno 5 punti di differenza e un'ora di dati
            if (scesa < 5 || tempoCollegato < UN_ORA)
            {
                return -1;
            }
            return (double)tempoCollegato / scesa * 100 / UN_ORA;
        }

        // Quanto tempo rimane ("circa 6 ore rimaste")
        public string TempoStimato(string nome)
        {
            long tempoCollegato;
            int scesa;
            if (!CalcolaScarica(nome, out tempoCollegato, out scesa))
            {
                return Testi.T("calcolo");
            }

            // Servono almeno 3 punti di differenza e mezz'ora di dati
            if (scesa < 3 || tempoCollegato < 30 * UN_MINUTO)
            {
                return Testi.T("calcolo");
            }

            int adesso = UltimaLettura(nome).P;
            double tempoPerPunto = (double)tempoCollegato / scesa;
            double oreRimaste = adesso * tempoPerPunto / UN_ORA;
            return TestoTempoRimasto(oreRimaste);
        }

        // Trasforma le ore rimaste in testo
        public static string TestoTempoRimasto(double oreRimaste)
        {
            if (oreRimaste < 1)
            {
                return Testi.T("meno_1_ora");
            }
            if (oreRimaste < 1.5)
            {
                return Testi.T("circa_1_ora");
            }
            if (oreRimaste < 48)
            {
                return Testi.T("circa_ore", Math.Round(oreRimaste));
            }
            return Testi.T("circa_giorni", Math.Round(oreRimaste / 24));
        }

        // Trasforma una durata in ore in testo ("18 ore", "3 giorni")
        public static string TestoDurata(double ore)
        {
            if (ore < 48)
            {
                return Testi.T("ore", Math.Round(ore));
            }
            return Testi.T("giorni", Math.Round(ore / 24));
        }
    }
}
