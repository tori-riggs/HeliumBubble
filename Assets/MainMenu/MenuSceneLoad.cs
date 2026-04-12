using UnityEngine;
using UnityEngine.SceneManagement;

namespace MainMenu
{
    public class MenuSceneLoad : MonoBehaviour
    {
        public void LoadGameScene()
        {
            SceneManager.LoadScene(2);
            Debug.Log("Loaded Game Scene");
        }
    }
}