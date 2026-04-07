using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Serialization;
using MainMenu;

namespace RhythmGameAssets.Scripts
{
    public class ChartParser : MonoBehaviour
    {
        private enum Instruments
        {
            GUITAR1,
            GUITAR2,
            BASS,
            DOUBLEBASS,
            KEYBOARD,
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

        // Directory of all songs
        [SerializeField] private string songsDir = @"Assets/RhythmGameAssets/Resources/Songs";
        [SerializeField] private string chartFile = @"notes.chart"; // notes.chart file name
        [SerializeField] private string chartPath;

        [SerializeField] public string selectedSongName; // Selected song

        // [SerializeField] public Song selectedSong = new Song(); // Selected song
        // TODO audio should be stored alongside the files
        [SerializeField] private string audioPath;

        // Note: might change in the middle of the song!
        private int _noteCount = 0;
        // public List<ChartNote> Notes = new();

        private MainMenu.MainMenu _mainMenu; // Main Menu

        // Temporary CurrentChart field.
        public Song CurrentSong { get; private set; }
        public Chart CurrentChart { get; private set; }

        // String: Section, Array: [start, end]

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            // Alternative: ToLower().FirstCharacterToUpper()
            // Difficulty = nameof(Difficulties.HARD); // default difficulty
            // Instrument = nameof(Instruments.GUITAR1); // default instrument

            // Get full path of the Songs directory
            var selectedSong = _mainMenu.SongList[selectedSongName];

            songsDir = Path.GetFullPath(songsDir);
            // TODO: Support song selection
            var selectedDir = TestChartParser(); // testing
            // Get the file path of the notes.chart file
            chartPath = Path.Join(selectedDir, chartFile); // Songs/<song_name>/notes.chart
            // HeliumBubbleRhythmGame/Assets/RhythmGameAssets/Resources/Songs/notes.chart
            if (File.Exists(chartPath))
            {
                Console.WriteLine("success");
            }
            else
            {
                Console.WriteLine("Specified file does not " +
                                  "exist in the current directory.");
                throw new FileNotFoundException(chartPath);
            }

            // _mainMenu.SongList()
            CurrentSong = new Song(selectedSongName);
            Chart chart = new Chart(selectedSongName);
            try
            {
                Boolean parsingMode = false;
                Boolean syncTrackMode = false;
                using (StreamReader reader = new StreamReader(chartPath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("{") || 
                            (line.StartsWith("}") && !parsingMode))
                        {
                            continue;
                        }

                        if (parsingMode) // parse chart notes
                        {
                            if (line.Trim().StartsWith("}"))
                            {
                                CurrentChart = chart;
                                AddChartSong(CurrentSong, CurrentChart); // add chart to song's chart dictionary
                                parsingMode = false;
                            }
                            ParseNotes(line.Trim(), chart);
                        }
                        else if (line.Trim().StartsWith("Off"))
                        {
                            string[] offsetLine = line.Trim().Split(' ');
                            chart.Offset = float.Parse(offsetLine[2]);
                        }
                        else if (syncTrackMode)
                        {
                            line = line.Trim();
                            string[] lineSplit = line.Split(' ');
                            ParseSyncTrack(lineSplit, chart); // get Time Signature
                            line = reader.ReadLine(); // read next line
                            if (line == null)
                            {
                                throw new IOException("Time Signature or BPM expected.");
                            }

                            lineSplit = line.Trim().Split(' ');
                            ParseSyncTrack(lineSplit, chart); // get BPM
                            if ((line = reader.ReadLine()) != null
                                && line.StartsWith("}"))
                                syncTrackMode = false; // turn off SyncTrack parsing
                        }
                        else if (line.StartsWith("["))
                        {
                            if (line.Equals("[SyncTrack]"))
                            {
                                reader.ReadLine(); // skip "{"
                                // THIS ASSUME THERE'S A CONSTANT BPM AND TIME SIGNATURE
                                // TODO: worry about changing bpms/time signatures???
                                // Defaults to 2 (x/4) if unspecified
                                syncTrackMode = true;
                            }
                            else // Charting part
                            {
                                // Code to remove the empty entries by CBinet:
                                // https://stackoverflow.com/a/44868165/16776633
                                string[] header = Regex.Split(line,
                                    @"([A-Z][^A-Z\]]+)").Where(s => s != string.Empty).ToArray();
                                // Check which chart the notes are for
                                if (Enum.IsDefined(typeof(Difficulties), header[1].ToUpper())
                                    && Enum.IsDefined(typeof(Instruments), header[2].ToUpper()))
                                {
                                    chart.Difficulty = header[1].ToUpper();
                                    chart.Instrument = header[2].ToUpper();
                                    reader.ReadLine(); // skip '{'
                                    parsingMode = true;
                                }
                            }
                        }
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("The chart file could not be read: ");
                Console.WriteLine(e.Message);
                throw new IOException(e.Message);
            }

            AddChartSong(CurrentSong, chart); // add chart to song's chart dictionary
            CurrentChart = chart; // set current chart as the chart
        }

        // Update is called once per frame
        void Update()
        {
        }

        // TODO documentation
        /// <summary>
        /// Assumes position 0
        /// Chart is edited directly.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="chart"></param>
        private void ParseSyncTrack(string[] line, Chart chart)
        {
            // TimeSignature = 4; // default
            switch (line[2])
            {
                // TODO error handling
                case "TS":
                    // <Position> = TS <Numerator> <Denominator exponent>
                    // Position of 0 is assumed
                    int timeSignature = int.Parse(line[3]);
                    // x/4 is assumed
                    // TODO figure out if optional denom exponent is needed
                    chart.TimeSignature = timeSignature;
                    break;
                case "B":
                    // <Position> = B <Tempo>
                    // Position of 0 is assumed
                    float bpm = float.Parse(line[3]) / 1000;
                    chart.BPM = bpm;
                    break;
            }
        }

        private void ParseNotes(string line, Chart chart)
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

                    ChartNote chartNote = new ChartNote(_noteCount++, noteDirection, position, length);
                    chart.Notes.Add(chartNote);
                    if (chart.Notes.Count > 1)
                    {
                        ChartNote prev = chart.Notes[chartNote.ID - 1];
                        prev.Next = chartNote;
                    }
                }
            }
        }

        /// <summary>
        /// Add chart to a song chart list through Song.AddChart()
        /// </summary>
        /// <param name="song">Song object that chart belongs to</param>
        /// <param name="chart">Chart object for song</param>
        public void AddChartSong(Song song, Chart chart)
        {
            song.AddChart(chart.Difficulty, chart);
        }

        private string TestChartParser()
        {
            // TODO: Test ChartParser.AddChart
            selectedSongName = "Mirage";
            var selectedDir = Path.Join(songsDir, selectedSongName);
            // var fullPath = Path.Join(selectedDir, chartFile);
            return selectedDir;
        }
    }
}