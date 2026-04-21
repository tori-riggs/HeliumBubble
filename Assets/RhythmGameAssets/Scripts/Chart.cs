using System.Collections.Generic;
using System.Linq;

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

        public int NoteCount => Notes.Count;
        public int HoldNoteCount => Notes.Count(n => n.Length > 0);

        public float PointsPerNote
        {
            get
            {
                int mult = Difficulty switch
                {
                    "EASY" => 1,
                    "MEDIUM" => 2,
                    "EXPERT" => 3,
                    _ => 1
                };
                int denom = NoteCount + HoldNoteCount;
                return denom > 0 ? mult * 100000f / denom : 0f;
            }
        }

        public Chart(string songName)
        {
            SongName = songName;
            Notes = new List<ChartNote>();
        }
    }
}