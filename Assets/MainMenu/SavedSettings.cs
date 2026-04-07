using UnityEngine;
using RhythmGameAssets.Scripts;
using System.Collections.Generic;
using System.IO;

namespace MainMenu
{
    [CreateAssetMenu(fileName = "SavedSettings", menuName = "Scriptable Objects/SavedSettings")]
    public class SavedSettings : ScriptableObject
    {
        public string Instrument { get; set; }
        public string Difficulty { get; set; }
        public Song SelectedSong { get; set; }
    }
}
