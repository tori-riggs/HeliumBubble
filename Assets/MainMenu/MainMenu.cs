using System;
using UnityEngine;
using Unity.UI;
using RhythmGameAssets.Scripts;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.VectorGraphics;
using UnityEngine.SceneManagement;

namespace MainMenu
{
    public class MainMenu : MonoBehaviour
    {
        public SavedSettings SavedSettings; // song settings
        public ChartParser ChartParser;

        // private SongManager _songManager;
        // TODO update chart parser
        [SerializeField] private string songsDir = @"Assets/RhythmGameAssets/Resources/Songs";
        // public Dictionary<string, Song> SongList { get; private set; }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // _songManager = FindFirstObjectByType<SongManager>();
            SavedSettings = ScriptableObject.CreateInstance<SavedSettings>();
            ChartParser = FindFirstObjectByType<ChartParser>();
            if (ChartParser.didAwake)
                Debug.Log("ChartParser did wake.");
            DefaultSelection(); // TODO remove debug function
            Debug.Log("Success.");
            // SceneManager.LoadScene("Scenes/RythmnGame");
        }
        
        // Update is called once per frame
        void Update()
        {

        }
        
        /// <summary>
        /// Default selection for debugging purposes.
        /// </summary>
        private void DefaultSelection()
        {
            var selectedSong = SongManager.Instance.SongList["Mirage"];
            var difficulty = "EXPERT";
            var instrument = "SINGLE";
            SavedSettings.SelectedSong = selectedSong;
            SavedSettings.Difficulty = difficulty;
            SavedSettings.Instrument = instrument;
            // SavedSettings.SelectedChart = selectedSong.GetChart(difficulty, instrument);
        }
    }
}