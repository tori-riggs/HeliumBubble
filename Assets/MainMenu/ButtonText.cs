using UnityEngine;
using TMPro;

namespace MainMenu
{
    public class ButtonText : MonoBehaviour
    {
        public int difficultyIdx; // index of the difficulty
        public int instrumentIdx; // index of the instrument
        public GameObject instrumentButton;
        public TextMeshProUGUI instrumentText;

        // void Awake()
        // {
        //     instrumentText.text = SavedSettings.Instance.Difficulty;
        // }
        
        public void SetInstrument(int val)
        {
            string[] instruments = { "SINGLE", "KEYBOARD", "DOUBLEBASS" };
            if (instrumentIdx + val == instruments.Length)
            {
                instrumentIdx = 0;
            }
            else
            {
                instrumentIdx += val;
            }
            instrumentText.text = instruments[instrumentIdx];
        }
    }
}