using System;
using UnityEngine;
using Unity.UI;
using RhythmGameAssets.Scripts;
using System.Collections.Generic;
using System.IO;
using TMPro;

namespace MainMenu
{
    public class MainMenu : MonoBehaviour
    {
        public SavedSettings SavedSettings; // song settings
        // TODO update chart parser
        public Dictionary<string, Song> SongList { get; private set; }
        [SerializeField] private string songsDir = @"Assets/RhythmGameAssets/Resources/Songs";
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            SavedSettings = ScriptableObject.CreateInstance<SavedSettings>();
            SongList = new Dictionary<string, Song>();
            CreateSongList();
            DefaultSelection(); // TODO remove debug function
        }
        
        // Update is called once per frame
        void Update()
        {

        }

        private void CreateSongList()
        {
            songsDir = Path.GetFullPath(songsDir);
            var songs = Directory.GetDirectories(songsDir);
            // Songs come from the Songs directory
            foreach (var s in songs)
            {
                // var currSongDir = Path.Join(songsDir, s);
                var songInfo = GetSongInfo(s);
                var songName = songInfo[0];
                var artist = songInfo[1]; // get artist name
                if (artist == "")
                    artist = "Unknown";
                
                Song song = new Song(songName, artist);
                // Key: Song Name, Value: Song object
                SongList.Add(song.Name, song);
            }
        }

        private List<string> GetSongInfo(string currSongDir)
        {
            if (!Directory.Exists(currSongDir))
            {
                throw new DirectoryNotFoundException($"Song directory not found: {currSongDir}");
            }
            var files = Directory.GetFiles(songsDir);
            // Read song.ini
            var songInfoFile = Path.Join(currSongDir, "song.ini");
            if (!File.Exists(songInfoFile))
            {
                // technically you don't need this but work on error handling later
                throw new FileNotFoundException($"Song info file not found: {songInfoFile}");
            }
            
            List<string> songInfo = new List<string>();
            try
            {
                bool firstLine = true;
                using (var reader = new StreamReader(songInfoFile))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Check that file starts with [song] / [Song]
                        if (firstLine)
                        {
                            if (!(line.Equals("[song]") || line.Equals("[Song]")))
                                throw new Exception("song.ini must start with [song] or [Song]");
                            firstLine = false; // song.ini is valid, move onto next line
                        } else if (line.StartsWith("name"))
                        {
                            // name = song name
                            var songLine = line.Trim().Split('=');
                            string songName = songLine[1].Trim();
                            songInfo.Add(songName);
                        } else if (line.StartsWith("artist"))
                        {
                            // artist = artist name
                            var artistLine = line.Trim().Split('=');
                            string artist = artistLine[1].Trim();
                            songInfo.Add(artist);
                            // return artist;
                        }
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("The song.ini file could not be read: ");
                Console.WriteLine(e.Message);
            }

            return songInfo;
        }
        
        private void DefaultSelection()
        {
            SavedSettings.SelectedSong = SongList["Mirage"];
            SavedSettings.Difficulty = "Hard";
            SavedSettings.Difficulty = "Expert";
            // SavedSettings.SelectedChart = SongList["Mirage"];
        }
    }
}