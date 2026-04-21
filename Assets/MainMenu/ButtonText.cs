using UnityEngine;
using System.Collections.Generic;
using RhythmGameAssets.Scripts;
using TMPro;

namespace MainMenu
{
    public class ButtonText : MonoBehaviour
    {
        public int difficultyIdx; // index of the difficulty
        public int instrumentIdx; // index of the instrument
        public GameObject instrumentButton;
        public TextMeshProUGUI instrumentText;
        public GameObject difficultyButton;
        public TextMeshProUGUI difficultyText;
        public TextMeshProUGUI error;
        // private string[] _difficulties = { "EASY", "MEDIUM", "Expert" };
        // private Song song = SavedSettings.Instance.SelectedSong;
        private List<string> _difficulties = new List<string>();
        
        public void SetInstrument(int val)
        {
            // TODO error handling for if difficulty doesn't exist for an instrument
            var instruments = SavedSettings.Instance.SelectedSong.Instruments;
            if (instrumentIdx + val == instruments.Count)
            {
                instrumentIdx = 0;
            }
            else
            {
                instrumentIdx += val;
            }
            // Set Instrument
            instrumentText.text = instruments[instrumentIdx];
            // get available difficulties
            _difficulties = 
                SavedSettings.Instance.SelectedSong.GetDifficulties(instruments[instrumentIdx]);
            // TODO: force order
            if (!_difficulties.Contains(difficultyText.text))
            {
                difficultyText.text = _difficulties[0];
                difficultyIdx = 0;
            }

            SavedSettings.Instance.Instrument = instrumentText.text;
            SavedSettings.Instance.Difficulty = difficultyText.text;
        }

        public void SetDifficulty(int val)
        {
            if (_difficulties.Count == 0)
            {
                SetInstrument(0);
            }
            // TODO: Show only difficulties that the instrument has
            if (difficultyIdx + val == _difficulties.Count)
            {
                difficultyIdx = 0;
            }
            else
            {
                difficultyIdx += val;
            }
            difficultyText.text = _difficulties[difficultyIdx];
            SavedSettings.Instance.Instrument = instrumentText.text;
            SavedSettings.Instance.Difficulty = difficultyText.text;
        }
    }
}