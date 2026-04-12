using System;
using UnityEngine;
using RhythmGameAssets.Scripts;
using System.Collections.Generic;
using System.IO;

namespace MainMenu
{
    public class SongManager : MonoBehaviour
    {
        [SerializeField] private string _songsDir = @"Assets/RhythmGameAssets/Resources/Songs";
        public Dictionary<string, Song> SongList = new Dictionary<string, Song>();

        // Singleton instance
        public static SongManager Instance { get; private set; }
        // public ChartParser ChartParser { get; private set; }
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(this);
            else
            {
                Instance = this;
                CreateSongList();
                DontDestroyOnLoad(this);
            }
        }
        private void CreateSongList()
        {
            _songsDir = Path.GetFullPath(_songsDir);
            var songs = Directory.GetDirectories(_songsDir);
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
                // TODO call chart parser?
                
            }
        }
        
        private List<string> GetSongInfo(string currSongDir)
        {
            if (!Directory.Exists(currSongDir))
            {
                throw new DirectoryNotFoundException($"Song directory not found: {currSongDir}");
            }
            var files = Directory.GetFiles(_songsDir);
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
                            var songLine = line.Trim().Split('=');
                            string songName = songLine[1].Trim();
                            songInfo.Add(songName);
                        } else if (line.StartsWith("artist"))
                        {
                            // artist = artist name
                            var artistLine = line.Trim().Split('=');
                            string artist = artistLine[1].Trim();
                            songInfo.Add(artist);
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
    }
}
