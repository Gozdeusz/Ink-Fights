using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalUIManager : MonoBehaviour
{
    public static GlobalUIManager Instance; // Dodajemy Singleton dla ³atwego dostêpu

    [Header("UI Containers")]
    [SerializeField] private GameObject mainMenuContainer;
    [SerializeField] private GameObject gameHudContainer;
    // Loading Screen jest niezale¿ny od logiki scen (pojawia siê "pomiêdzy"), 
    // wiêc nie musimy go tu koniecznie przypisywaæ, UILoadingScreen sam sob¹ zarz¹dzi.

    private void Awake()
    {
        // --- ZMIANA: Singleton + DontDestroyOnLoad ---
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Chronimy CA£Y MainCanvas (Root)
        }
        else
        {
            Destroy(gameObject); // Jeœli wrócimy do Menu, niszczymy duplikat Canvasa
            return;
        }
        // ---------------------------------------------

        SceneManager.activeSceneChanged += OnSceneChanged;
        CheckCurrentScene(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        CheckCurrentScene(newScene);
    }

    private void CheckCurrentScene(Scene scene)
    {
        string sceneName = scene.name;

        if (sceneName == "MainMenuScene")
        {
            mainMenuContainer.SetActive(true);
            gameHudContainer.SetActive(false);
        }
        else if (sceneName == "GameScene")
        {
            mainMenuContainer.SetActive(false);
            gameHudContainer.SetActive(true);
        }
    }
}
