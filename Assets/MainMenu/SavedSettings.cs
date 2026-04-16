using UnityEngine;
using RhythmGameAssets.Scripts;

namespace MainMenu
{
    public enum ControlScheme
    {
        WASD,
        DFJK,
        ZXCV,
    };

    public class SavedSettings : MonoBehaviour
    {
        public string Difficulty { get; set; }
        public string Instrument { get; set; }
        public Song SelectedSong { get; set; }
        public Chart SelectedChart { get; set; }
        public ControlScheme CurrentControlScheme { get; set; }
        public static SavedSettings Instance { get; set; }

        void Start()
        {
            if (Instance != null && Instance != this)
                Destroy(this);
            else
            {
                // default WASD
                this.CurrentControlScheme = ControlScheme.WASD;

                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
    }
    
}
