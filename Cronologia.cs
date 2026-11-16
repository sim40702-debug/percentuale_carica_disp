// Cronologia.cs
// Si ricorda la batteria nel tempo, per:
// - il grafico delle ultime 24 ore
// - il tempo stimato ("circa 6 ore rimaste")

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
        const long UN_ORA = 60 * UN_MINUTO;
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

        // I punti delle ultime 24 ore (per il grafico)
        public List<Lettura> Ultime24Ore(string nome)
        {
            List<Lettura> risultato = new List<Lettura>();
            if (!dati.ContainsKey(nome))
            {
                return risultato;
            }

            long inizio = Adesso() - 24 * UN_ORA;
            foreach (Lettura l in dati[nome])
            {
                if (l.T >= inizio)
                {
                    risultato.Add(l);
                }
            }
            return risultato;
        }

        // Quanto tempo rimane, guardando quanto scende la batteria dall'ultima carica
        public string TempoStimato(string nome)
        {
            if (!dati.ContainsKey(nome) || dati[nome].Count < 2)
            {
                return "Calcolo in corso…";
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

            // 2. Quanto tempo è stato collegato da allora (senza contare le pause)
            long tempoCollegato = 0;
            for (int i = inizio + 1; i < punti.Count; i++)
            {
                long pausa = punti[i].T - punti[i - 1].T;
                if (pausa < PAUSA_MASSIMA)
                {
                    tempoCollegato = tempoCollegato + pausa;
                }
            }

            // 3. Quanto è scesa
            Lettura ultima = punti[punti.Count - 1];
            int scesa = punti[inizio].P - ultima.P;

            // Servono almeno 3 punti di differenza e mezz'ora di dati
            if (scesa < 3 || tempoCollegato < 30 * UN_MINUTO)
            {
                return "Calcolo in corso…";
            }

            // 4. Tempo per ogni punto % e tempo rimasto
            double tempoPerPunto = (double)tempoCollegato / scesa;
            double oreRimaste = ultima.P * tempoPerPunto / UN_ORA;

            if (oreRimaste < 1)
            {
                return "meno di 1 ora rimasta";
            }
            if (oreRimaste < 1.5)
            {
                return "circa 1 ora rimasta";
            }
            if (oreRimaste < 48)
            {
                return "circa " + Math.Round(oreRimaste) + " ore rimaste";
            }
            return "circa " + Math.Round(oreRimaste / 24) + " giorni rimasti";
        }
    }
}
