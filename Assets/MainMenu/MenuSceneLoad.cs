using UnityEngine;
using UnityEngine.SceneManagement;

namespace MainMenu
{
    public class MenuSceneLoad : MonoBehaviour
    {
        public void LoadGameScene()
        {
            Debug.Log($"SavedSettings Difficulty: {SavedSettings.Instance.Difficulty}");
            Debug.Log($"SavedSettings Instrument: {SavedSettings.Instance.Instrument}");
            SceneManager.LoadScene(2);
            Debug.Log("Loaded Game Scene");
        }
    }
}