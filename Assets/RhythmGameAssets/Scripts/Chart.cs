using System.Collections.Generic;

namespace RhythmGameAssets.Scripts
{
    public class Chart
    {
        public string SongName { get; private set; }
        // Array with instrument and difficulty?
        public string Difficulty { get; set; } // uppercase
        public string Instrument { get; set; } // uppercase
        public float Offset { get; set; }
        public int TimeSignature { get; set; }
        // <Position> = B <Tempo>
        public float BPM { get; set; }
        public List<ChartNote> Notes { get; private set; }

        // public Chart(string songName, string instrument, string difficulty)
        // {
        //     SongName = songName;
        //     Instrument = instrument;
        //     Difficulty = difficulty;
        //     Notes = new List<ChartNote>();
        // }

        public Chart(string songName)
        {
            SongName = songName;
            Notes = new List<ChartNote>();
        }
    }
}