// Statistiche.cs
// Si ricorda ogni volta che carichi un dispositivo, per sapere:
// - quante volte l'hai caricato questo mese
// - quanto dura in media una carica completa
// - se la batteria si scarica molto più in fretta di prima (batteria vecchia)

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BatterieInBarra
{
    // Una carica: quando (T, in millisecondi) e quanto sarebbe durata una carica completa prima (in ore, 0 = sconosciuto)
    public class Carica
    {
        public long T { get; set; }
        public double OrePiene { get; set; }
    }

    // Quello che salviamo nel file
    public class DatiStatistiche
    {
        public Dictionary<string, List<Carica>> Cariche { get; set; } = new Dictionary<string, List<Carica>>();
        public Dictionary<string, long> UltimoAvvisoSalute { get; set; } = new Dictionary<string, long>();
    }

    public class Statistiche
    {
        const int MASSIMO_CARICHE = 100;   // teniamo le ultime 100 cariche per dispositivo
        const long TRENTA_GIORNI = 30L * 24 * 60 * 60 * 1000;

        DatiStatistiche dati = new DatiStatistiche();
        string file;

        public Statistiche()
        {
            file = Path.Combine(Impostazioni.Cartella(), "statistiche.json");
            try
            {
                dati = JsonSerializer.Deserialize<DatiStatistiche>(File.ReadAllText(file));
            }
            catch (Exception)
            {
                // il file non c'è ancora
            }
            if (dati == null)
            {
                dati = new DatiStatistiche();
            }
        }

        void Salva()
        {
            File.WriteAllText(file, JsonSerializer.Serialize(dati));
        }

        // Il dispositivo è stato appena caricato
        public void RegistraCarica(string nome, double orePiene)
        {
            if (!dati.Cariche.ContainsKey(nome))
            {
                dati.Cariche[nome] = new List<Carica>();
            }
            List<Carica> cariche = dati.Cariche[nome];

            cariche.Add(new Carica { T = Cronologia.Adesso(), OrePiene = orePiene });
            while (cariche.Count > MASSIMO_CARICHE)
            {
                cariche.RemoveAt(0);
            }
            Salva();
        }

        // Quante volte è stato caricato questo mese
        public int CaricheQuestoMese(string nome)
        {
            if (!dati.Cariche.ContainsKey(nome))
            {
                return 0;
            }

            DateTime oggi = DateTime.Now;
            int conta = 0;
            foreach (Carica c in dati.Cariche[nome])
            {
                DateTime quando = DateTimeOffset.FromUnixTimeMilliseconds(c.T).LocalDateTime;
                if (quando.Year == oggi.Year && quando.Month == oggi.Month)
                {
                    conta = conta + 1;
                }
            }
            return conta;
        }

        // Le durate conosciute (in ore) di una carica completa, dalla più vecchia alla più nuova
        List<double> Durate(string nome)
        {
            List<double> durate = new List<double>();
            if (dati.Cariche.ContainsKey(nome))
            {
                foreach (Carica c in dati.Cariche[nome])
                {
                    if (c.OrePiene > 0)
                    {
                        durate.Add(c.OrePiene);
                    }
                }
            }
            return durate;
        }

        // Quanto dura in media una carica completa (in ore). -1 se non lo sappiamo ancora.
        public double DurataMedia(string nome)
        {
            List<double> durate = Durate(nome);
            if (durate.Count == 0)
            {
                return -1;
            }

            double somma = 0;
            foreach (double d in durate)
            {
                somma = somma + d;
            }
            return somma / durate.Count;
        }

        // La batteria si scarica molto più in fretta di prima?
        // Confrontiamo le ultime 2 cariche con la media di quelle prima (ne servono almeno 3 prima).
        public bool BatteriaPeggiorata(string nome)
        {
            List<double> durate = Durate(nome);
            if (durate.Count < 5)
            {
                return false;
            }

            double sommaPrima = 0;
            for (int i = 0; i < durate.Count - 2; i++)
            {
                sommaPrima = sommaPrima + durate[i];
            }
            double mediaPrima = sommaPrima / (durate.Count - 2);
            double mediaAdesso = (durate[durate.Count - 1] + durate[durate.Count - 2]) / 2;

            // Peggiorata se adesso dura meno del 70% di prima
            return mediaAdesso < mediaPrima * 0.7;
        }

        // Possiamo avvisare per la salute? (al massimo una volta ogni 30 giorni per dispositivo)
        public bool PuoiAvvisareSalute(string nome)
        {
            long ultimo;
            if (dati.UltimoAvvisoSalute.TryGetValue(nome, out ultimo) && Cronologia.Adesso() - ultimo < TRENTA_GIORNI)
            {
                return false;
            }
            dati.UltimoAvvisoSalute[nome] = Cronologia.Adesso();
            Salva();
            return true;
        }

        public void Cancella()
        {
            dati = new DatiStatistiche();
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
