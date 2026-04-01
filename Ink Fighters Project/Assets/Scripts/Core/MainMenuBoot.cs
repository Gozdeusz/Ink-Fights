using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuBoot : MonoBehaviour
{
    void Start()
    {
        if (!IsSceneLoaded("UI"))
        {
            SceneManager.LoadSceneAsync("UI", LoadSceneMode.Additive);
        }
    }

    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == sceneName) return true;
        }
        return false;
    }
}
