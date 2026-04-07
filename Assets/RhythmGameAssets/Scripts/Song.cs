using System;
using System.Collections.Generic;

namespace RhythmGameAssets.Scripts
{
    public class Song
    {
        public string Name { get; set; }
        public string Artist { get; set; }

        // Dictionary of all song charts
        // Key: Difficulty, Value: List of charts
        private Dictionary<string, List<Chart>> _charts = new();

        public Song(string name)
        {
            Name = name;
        }
        
        public Song(string name, string artist)
        {
            Name = name;
            Artist = artist;
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
    }
}