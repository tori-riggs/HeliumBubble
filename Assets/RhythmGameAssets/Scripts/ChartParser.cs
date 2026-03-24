using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Serialization;

namespace RhythmGameAssets.Scripts
{
    public class ChartParser : MonoBehaviour
    {
        private enum Instruments
        {
            GUITAR1,
            GUITAR2,
            BASS,
            PIANO,
            DRUMS,
            VOCALS,
            SINGLE // parsing purposes
        }
        private enum Difficulties
        {
            EASY,
            MEDIUM, 
            HARD,
            EXPERT // will be unused in our version but just there for parsing purposes
        }

        // private enum NoteType
        // {
        //     LEFT, // 1
        //     UP, // 2
        //     DOWN, // 3
        //     RIGHT // 4
        // }
        // private string[] _noteType = {"LEFT", "UP", "DOWN", "RIGHT"}; 
        // TODO: Find a way to have a "Resources/Songs/<song-name>/notes.chart" file structure
        // TODO: Why do we have Assets/RhythmGameAssets?
        // Directory of all songs
        [SerializeField] private string songsDir = @"Assets/RhythmGameAssets/Resources/Songs";
        [SerializeField] private string chartFile = @"notes.chart"; // notes.chart file name
        [SerializeField] private string chartPath;
        [SerializeField] private string audioPath;

        public string Difficulty { get; private set; }
        public string Instrument { get; private set; }
        public float Offset { get; private set; }
        // <Position> = TS <Numerator> <Denominator exponent> 
        // Note: might change in the middle of the song!
        public int TimeSignature { get; private set; }
        // <Position> = B <Tempo>
        public float BPM { get; private set; } // tempo
        private int noteCount = 0;
        public List<ChartNote> Notes = new();

        // String: Section, Array: [start, end]
        // private Dictionary<string, Array> sections = new();
    
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            // Get full path of the Songs directory
            // Alternative: ToLower().FirstCharacterToUpper()
            Difficulty = nameof(Difficulties.HARD); // default difficulty
            Instrument = nameof(Instruments.GUITAR1); // default instrument
            songsDir = Path.GetFullPath(songsDir);
            // TODO: Add a way to get Songs/<song-name>/notes.chart. Probably would just be using the song name or a regex but worry about that later.
            // Get the file path of the notes.chart file
            chartPath = Path.Join(songsDir, "notes.chart");
            // /Users/nyla/Github Projects/HeliumBubbleRhythmGame/Assets/RythmnGameAssets/Resources/Songs/notes.chart
            if (File.Exists(chartPath))
            {
                // TODO remove if unnecessary
                Console.WriteLine("success");
            }
            else {
                Console.WriteLine("Specified file does not "+
                                  "exist in the current directory.");
            }

            try
            {
                // using (FileStream fs = File.OpenRead(chartPath))
                Boolean parsingMode = false;
                Boolean syncTrackMode = false;
                using (StreamReader reader = new StreamReader(chartPath))
                {
                    // byte[] b = new byte[1024];
                    // UTF8Encoding temp = new UTF8Encoding(true);
                    // int readLen;
                    // while ((readLen = fs.Read(b, 0, b.Length)) > 0) 
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("{") || line.StartsWith("}"))
                        {
                            continue;
                        }
                        if (parsingMode)
                        {
                            ParseNotes(line.Trim());
                        }
                        else if (syncTrackMode)
                        {
                            line = line.Trim();
                            string[] lineSplit = line.Split(' ');
                            ParseSyncTrack(lineSplit); // get Time Signature
                            line = reader.ReadLine(); // read next line
                            if (line == null)
                            {
                                throw new IOException("Time Signature or BPM expected.");
                            }
                            lineSplit = line.Trim().Split(' ');
                            ParseSyncTrack(lineSplit); // get BPM
                            if ((line = reader.ReadLine()) != null 
                                    && line.StartsWith("}"))
                                syncTrackMode = false; // turn off SyncTrack parsing
                        }
                        // if (!parsingMode && line.StartsWith("Offset: "))
                        else if (line.StartsWith("["))
                        {
                            if (line.Equals("[SyncTrack]"))
                            {
                                reader.ReadLine(); // skip "{"
                                // THIS ASSUME THERE'S A CONSTANT BPM AND TIME SIGNATURE
                                // TODO: worry about changing bpms/time signatures???
                                // Defaults to 2 (x/4) if unspecified
                                syncTrackMode = true;
                                // TODO: BPM/Time Signature class?
                            }
                            else
                            {
                                // Code to remove the empty entries by CBinet:
                                // https://stackoverflow.com/a/44868165/16776633
                                string[] header = Regex.Split(line, 
                                    @"([A-Z][^A-Z\]]+)").Where(s => s != string.Empty).ToArray();
                                if (Enum.IsDefined(typeof(Difficulties), header[1].ToUpper()) 
                                    && Enum.IsDefined(typeof(Instruments), header[2].ToUpper()))
                                {
                                    // TODO getters and setters
                                    Difficulty = header[1].ToUpper();
                                    Instrument = header[2].ToUpper();
                                    reader.ReadLine(); // skip '{'
                                    parsingMode = true;
                                }
                            }
                        } else if (line.Trim().StartsWith("Off"))
                        {
                            string[] offsetLine = line.Trim().Split(' ');
                            Offset = float.Parse(offsetLine[2]);
                        }
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("The chart file could not be read: ");
                Console.WriteLine(e.Message);
            }
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        // TODO documentation
        /// <summary>
        /// Assumes position 0
        /// Global variables are changed so no need for return
        /// </summary>
        /// <param name="line"></param>
        private void ParseSyncTrack(string[] line)
        {
            // TimeSignature = 4; // default
            switch (line[2])
            {
                // TODO error handling
                case "TS":
                    // <Position> = TS <Numerator> <Denominator exponent>
                    // Position of 0 is assumed
                    TimeSignature = int.Parse(line[3]);
                    // x/4 is assumed
                    // TODO figure out if optional denom exponent is needed
                    break;
                case "B":
                    // <Position> = B <Tempo>
                    // Position of 0 is assumed
                    BPM = float.Parse(line[3]) / 1000;
                    break;
            }
        }
        private void ParseNotes(string line)
        {
            // <Position> = N <Type> <Length>
            string[] note = line.Split(' ');
            // Some charts include events in this section, ignore.
            if (Char.Parse(note[2]) == 'N')
            {
                // TODO error handling
                double position = double.Parse(note[0]); // tick position of note
                int typeNum = int.Parse(note[3]); // direction of note as int
                int length = int.Parse(note[4]); // length of note in ticks
                NoteDirection noteDirection; // corresponding direction of the note
                // Types 5 and 6 are just flags for existing notes. Can be ignored for this.
                if (typeNum is >= 0 and <= 4)
                {
                    // TODO error handling
                    // noteDirection = _noteType[typeNum];
                    // 0 = Left, 1 = Up, 2 = Down, 3 = Right
                    switch (typeNum)
                    {
                        case 0:
                            noteDirection = NoteDirection.Left;
                            break;
                        case 1:
                            noteDirection = NoteDirection.Up;
                            break;
                        case 2:
                            noteDirection = NoteDirection.Down;
                            break;
                        case 3:
                            noteDirection = NoteDirection.Right;
                            break;
                        // CloneHero has 5 lanes so just make the 4th type right
                        case 4:
                            noteDirection = NoteDirection.Right;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    ChartNote chartNote = new ChartNote(noteCount++, noteDirection, position, length);
                    Notes.Add(chartNote);
                    if (Notes.Count > 1)
                    {
                        ChartNote prev = Notes[chartNote.ID - 1];
                        prev.Next = chartNote;
                    }
                }
            }
        }
    }
}
