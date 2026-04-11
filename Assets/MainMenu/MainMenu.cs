using System;
using UnityEngine;
using Unity.UI;
using RhythmGameAssets.Scripts;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.VectorGraphics;
using UnityEngine.SceneManagement;
using Scene = Unity.VectorGraphics.Scene;

namespace MainMenu
{
    public class MainMenu : MonoBehaviour
    {
        // public SavedSettings SavedSettings; // song settings
        public ChartParser ChartParser;

        // private SongManager _songManager;
        // TODO update chart parser
        [SerializeField] private string songsDir = @"Assets/RhythmGameAssets/Resources/Songs";
        // public Dictionary<string, Song> SongList { get; private set; }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            StartMain();
        }
        
        // Update is called once per frame
        void Update()
        {

        }

        private void StartMain()
        {
            // _songManager = FindFirstObjectByType<SongManager>();
            // SavedSettings = ScriptableObject.CreateInstance<SavedSettings>();
            // SceneManager.LoadScene("Scenes/Startup");
            if (SongManager.Instance == null)
                throw new NullReferenceException("SongManager is null");
            DefaultSelection(); // TODO remove debug function
            Debug.Log($"SavedSettings Difficulty: {SavedSettings.Instance.Difficulty}");
            ChartParser = FindFirstObjectByType<ChartParser>();
            if (ChartParser.didAwake)
                Debug.Log("ChartParser did wake.");
            Debug.Log("Success.");
        }
        
        /// <summary>
        /// Default selection for debugging purposes.
        /// </summary>
        private void DefaultSelection()
        {
            var selectedSong = SongManager.Instance.SongList["Mirage"];
            Debug.Log("DefaultSelection Entered");
            var difficulty = "EXPERT";
            var instrument = "SINGLE";
            SavedSettings.Instance.SelectedSong = selectedSong;
            SavedSettings.Instance.Difficulty = difficulty;
            SavedSettings.Instance.Instrument = instrument;
            // SavedSettings.SelectedChart = selectedSong.GetChart(difficulty, instrument);
        }
    }
}