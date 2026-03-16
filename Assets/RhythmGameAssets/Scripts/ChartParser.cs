using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using System.Resources;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Unity.VisualScripting;

public class ChartParser : MonoBehaviour
{
    private enum Instrument
    {
        GUITAR1,
        GUITAR2,
        BASS,
        PIANO,
        DRUMS,
        VOCALS
    }
    private enum Difficulties
    {
        EASY,
        MEDIUM, 
        HARD,
        EXPERT // will be unused in our version but just there for parsing purposes
    }
    // TODO: Find a way to have a "Resources/Songs/<song-name>/notes.chart" file structure
    // TODO: Why do we have Assets/RhythmGameAssets?
    // Directory of all songs
    [SerializeField] private string songsDir = @"Assets/RhythmGameAssets/Resources/Songs";
    [SerializeField] private string chartFile = @"notes.chart"; // notes.chart file name
    [SerializeField] private string chartPath;
    [SerializeField] private string audioPath;

    [SerializeField] public string difficulty;
    [SerializeField] public string instrument;

    // String: Section, Array: [start, end]
    // private Dictionary<string, Array> sections = new();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get full path of the Songs directory
        // Alternative: ToLower().FirstCharacterToUpper()
        difficulty = nameof(Difficulties.HARD); // default difficulty
        instrument = nameof(Instrument.GUITAR1); // default instrument
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
            using (StreamReader reader = new StreamReader(chartPath))
            {
                // byte[] b = new byte[1024];
                // UTF8Encoding temp = new UTF8Encoding(true);
                // int readLen;
                // while ((readLen = fs.Read(b, 0, b.Length)) > 0) 
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // var line = temp.GetString(b, 0, readLen);
                    if (line.StartsWith("["))
                    {
                        // Code to remove the empty entries by CBinet:
                        // https://stackoverflow.com/a/44868165/16776633
                        string[] header = Regex.Split(line, 
                            @"([A-Z][^A-Z\]]+)").Where(s => s != string.Empty).ToArray();
                        if (Enum.IsDefined(typeof(Difficulties), header[1]) 
                            && Enum.IsDefined(typeof(Instrument), header[2]))
                        {
                            // TODO getters and setters
                            difficulty = header[1].ToUpper();
                            instrument = header[2].ToUpper();
                            reader.ReadLine(); // skip '{'
                            // TODO parse notes
                        }
                        // TODO parse SyncTrack stuff
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

    void ParseNotes(string line)
    {
        
    }
}
