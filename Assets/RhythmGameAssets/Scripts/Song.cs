using System;
using System.Collections.Generic;

namespace RhythmGameAssets.Scripts
{
    public class Song
    {
        public string Name { get; set; }
        public string Artist { get; set; }
        public List<string> Instruments { get; private set; }
        // public List<string[]> AvailableCharts { get; private set; }
        // public Dictionary<string, string> AvailableCharts { get; private set; } // Difficulty: Instrument

        // Dictionary of all song charts
        // Key: Difficulty, Value: List of charts
        private Dictionary<string, List<Chart>> _charts = new();
        // private Dictionary<string, Dictionary<string, Chart>> _charts = new(); // Difficulty: Instrument: Chart

        public Song(string name)
        {
            Name = name;
        }
        
        public Song(string name, string artist)
        {
            Name = name;
            Artist = artist;
            Instruments = new List<string>();
            // AvailableCharts = new List<string[]>();
        }

        /// <summary>
        /// Add a chart to the Song class
        /// </summary>
        /// <param name="difficulty"> Difficulty of the Chart</param>
        /// <param name="chart"> Chart file to add</param>
        public void AddChart(string difficulty, Chart chart)
        {
            if (!_charts.ContainsKey(difficulty))
            {
                // Make new difficulty
                _charts.Add(difficulty, new List<Chart>());
                // _charts.Add(difficulty, new Dictionary<string, Chart>());
                _charts[difficulty].Add(chart);
            }
            else
            {
                _charts[difficulty].Add(chart); // add chart to list
            }
        }

        public Chart GetChart(string difficulty, string instrument)
        {
            {
                var chart = _charts[difficulty.ToUpper()];
                foreach (var c in chart)
                {
                    if (c.Instrument == instrument.ToUpper())
                        return c;
                }
                // TODO exception handling
                return null;
            }
        }

        public List<string> GetInstruments()
        {
            // TODO error handling
            foreach (var c in _charts.Values)
            {
                foreach (var i in c)
                {
                    if (!Instruments.Contains(i.Instrument))
                        Instruments.Add(i.Instrument);
                }
            }
            return Instruments;
        }

        public List<string> GetDifficulties(string instrument)
        {
            List<string> difficulties = new List<string>();
            // Chart (difficulties[i] corresponds to index of sortOrder.
            List<int> sortOrder = new List<int>(); // sort order of difficulties[i]
            foreach (var d in _charts.Keys)
            {
                if (GetChart(d, instrument) != null)
                {
                    difficulties.Add(d);
                    switch (d)
                    {
                        case "EASY":
                            sortOrder.Add(0);
                            break;
                        case "MEDIUM":
                            sortOrder.Add(1);
                            break;
                        case "EXPERT":
                            sortOrder.Add(2);
                            break;
                    }
                }
            }
            bool swapped = true;
            for (int i = 0; i < difficulties.Count; i++)
            {
                swapped = false;
                for (int j = 0; j < difficulties.Count - i - 1; j++)
                {
                    if (sortOrder[j] > sortOrder[j + 1])
                    {
                        (sortOrder[j], sortOrder[j + 1]) = (sortOrder[j + 1], sortOrder[j]);
                        (difficulties[j], difficulties[j + 1]) = (difficulties[j + 1], difficulties[j]);
                        swapped = true;
                    }
                }

                if (!swapped) break;
            }
            return difficulties;
        }
    }
}