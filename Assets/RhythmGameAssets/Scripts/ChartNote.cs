using UnityEngine;
using UnityEngine.Serialization;

namespace RhythmGameAssets.Scripts
{
/*
 * A class for each note in the chart.
 */
    public class ChartNote
    {
        public int ID { get; set; }
        // public static int ID { get; set; }
        public NoteDirection Direction { get; set; }
        public int Position { get; set; } // the time the note appears in ticks
        public int Length { get; set; } // length in ticks
        public ChartNote Next { get; set; }

        public ChartNote(int id, NoteDirection direction, int position, int length)
        {
            ID = id;
            Direction = direction;
            Position = position;
            Length = length;
            Next = null;
        }
    }
}