using UnityEngine;
using RhythmGameAssets.Scripts;
using System.Collections.Generic;
using System.IO;

namespace MainMenu
{
    public class MainMenu : MonoBehaviour
    {
        public string Instrument { get; set; }
        public string Difficulty { get; set; }
        public Song SelectedSong { get; set; }
        // TODO update chart parser
        public Dictionary<string, Song> SongList { get; private set; }
        [SerializeField] private string songsDir = @"Assets/RhythmGameAssets/Resources/Songs";
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
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
                Song song = new Song(s);
                // Key: Song Name, Value: Song object
                SongList.Add(s, song);
            }
            // Song mirage = new Song("Mirage");
            // SongList.Add(mirage);
        }

        private void DefaultSelection()
        {
            SelectedSong = SongList["Mirage"];
        }
    }
}