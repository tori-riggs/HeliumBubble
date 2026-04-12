using UnityEngine;
using RhythmGameAssets.Scripts;
using System.Collections.Generic;
using System.IO;

namespace MainMenu
{
    // [CreateAssetMenu(fileName = "SavedSettings", menuName = "Scriptable Objects/SavedSettings")]
    public class SavedSettings : MonoBehaviour
    {
        public string Difficulty { get; set; }
        public string Instrument { get; set; }
        public Song SelectedSong { get; set; }
        public Chart SelectedChart { get; set; }
        public static SavedSettings Instance { get; set; }

        void Start()
        {
            if (Instance != null && Instance != this)
                Destroy(this);
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
    }
    
}
