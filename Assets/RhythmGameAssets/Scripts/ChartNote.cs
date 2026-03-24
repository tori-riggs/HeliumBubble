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
        public double Position { get; set; } // the time the note appears in ticks
        public double Length { get; set; } // length in ticks
        public ChartNote Next { get; set; }

        public ChartNote(int id, NoteDirection direction, double position, double length)
        {
            ID = id;
            Direction = direction;
            Position = position;
            Length = length;
            Next = null;
        }
        
        public override string ToString()
        {
            string nextText = Next != null ? Next.ID.ToString() : "null";
            return $"ChartNote[ID={ID}, Direction={Direction}, Position={Position}, Length={Length}, Next={nextText}]";
        }
    }
}