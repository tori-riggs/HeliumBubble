using UnityEngine;
using UnityEngine.SceneManagement;

namespace Startup
{
    public class SceneSetup : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // SceneManager.LoadScene(0);
            Debug.Log("Start");
            SceneManager.LoadScene("MainMenu");
            Debug.Log("MainMenu Loaded");
        }

        public void LoadGame()
        {
            Debug.Log("RhythmGame");
            SceneManager.LoadScene(2); // RhythmGame
            Debug.Log("RhythmGame Loaded");
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
