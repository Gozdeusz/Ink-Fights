using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIMainMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject modeSelectionPanel;
    [SerializeField] private GameObject gameplaySetupPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Setup References")]
    [SerializeField] private GameObject cpuDifficultyPanel; // Panel z wyborem trudnoœci (tylko dla PvAI)
    [SerializeField] private Image p1ColorPreview;
    [SerializeField] private Image p2ColorPreview;
    [SerializeField] private TMP_Text difficultyText;

    [SerializeField] private Sprite[] p1ColorIcon;
    [SerializeField] private Sprite[] p2ColorIcon;

    [Header("Configuration")]
    [SerializeField] private Color[] availableColors; // Przypisz kolory w inspektorze (np. Bia³y, Czerwony, Niebieski)
    [SerializeField] private string gameSceneName = "GameScene";

    // Stan lokalny menu
    private int p1ColorIdx = 0;
    private int p2ColorIdx = 0;
    private AIDifficulty currentDifficulty = AIDifficulty.Normal;

    // Inicjalizacja
    private void Start()
    {
        UpdatePreviews();
    }

    private void OnEnable()
    {
        ResetMenu();
    }

    private void ResetMenu()
    {
        // 1. Poka¿ g³ówny panel (Start/Exit)
        ShowPanel(mainMenuPanel);

        // 2. Opcjonalnie: Zresetuj wybory (jeœli chcesz)
         p1ColorIdx = 0;
        p2ColorIdx = (p1ColorIcon.Length > 1) ? 1 : 0;
        currentDifficulty = AIDifficulty.Normal;
         UpdatePreviews();
    }

    // --- NAWIGACJA ---

    // [MainMenuPanel] Start 
    public void OnClick_StartGame() // Guzik Start w MainMenu
    {
        ShowPanel(modeSelectionPanel);
    }

    public void OnClick_OpenSettings()
    {
        ShowPanel(settingsPanel);
    }

    // [MainMenuPanel] Exit
    public void OnClick_Exit()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }

    // [ModeSelectionPanel] Tryb gracz vs gracz
    public void OnClick_ModePvP()
    {
        GameSettings.Instance.CurrentMode = GameMode.PvP;
        OpenSetup();
    }

    // [ModeSelectionPanel] Tryb gracz vs AI
    public void OnClick_ModePvAI()
    {
        GameSettings.Instance.CurrentMode = GameMode.PvAI;
        OpenSetup();
    }

    // [ModeSelectionPanel] powrot do Main Menu
    public void OnClick_ReturnToMain()
    {
        ShowPanel(mainMenuPanel);
    }

    // [GameSettings] powrot do Mode Selection
    public void OnClick_ReturnToMode()
    {
        ShowPanel(modeSelectionPanel);
    }

    // --- SETUP PANEL ---
    // [GameSettings] Inicjalizacja panelu
    private void OpenSetup()
    {
        ShowPanel(gameplaySetupPanel);

        // Ukryj/Poka¿ wybór trudnoœci w zale¿noœci od trybu
        bool isPvAI = GameSettings.Instance.CurrentMode == GameMode.PvAI;
        cpuDifficultyPanel.SetActive(isPvAI);
    }

    // [GameSettings] rozpoczecie meczu
    public void OnClick_StartMatch()
    {
        // Zapisz dane do Singletona
        GameSettings.Instance.P1ColorIndex = p1ColorIdx;
        GameSettings.Instance.P2ColorIndex = p2ColorIdx;
        GameSettings.Instance.CPUDifficulty = currentDifficulty;

        // Za³aduj grê
        if (UILoadingScreen.Instance != null)
        {
            UILoadingScreen.Instance.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("Brak UILoadingScreen na scenie! Dodaj prefab.");
            // Fallback
            SceneManager.LoadScene(gameSceneName);
        }
    }

    // --- ZMIANA KOLORÓW I TRUDNOŒCI ---
    // [GameSettings] zmiana koloru  gracza 1
    public void OnClick_ChangeP1Color(int direction) // -1 lewo, 1 prawo
    {
        p1ColorIdx = GetNextValidIndex(p1ColorIdx, direction, p1ColorIcon.Length, p2ColorIdx);
        UpdatePreviews();
    }

    // [GameSettings] zmiana koloru  gracza 2
    public void OnClick_ChangeP2Color(int direction)
    {
        p2ColorIdx = GetNextValidIndex(p2ColorIdx, direction, p2ColorIcon.Length, p1ColorIdx);
        UpdatePreviews();
    }

    // [GameSettings] zmiana trudnosci AI
    public void OnClick_ChangeDifficulty(int direction)
    {
        // Proste prze³¹czanie miêdzy 2 trybami (mo¿na rozszerzyæ)
        if (currentDifficulty == AIDifficulty.Normal) currentDifficulty = AIDifficulty.Hard;
        else currentDifficulty = AIDifficulty.Normal;

        difficultyText.text = currentDifficulty.ToString();
    }

    // [GameSettings] update wyswietlanego koloru 
    private void UpdatePreviews()
    {
        if (p1ColorIcon.Length > 0 && p2ColorIcon.Length > 0)
        {
            p1ColorPreview.sprite = p1ColorIcon[p1ColorIdx];
            p2ColorPreview.sprite = p2ColorIcon[p2ColorIdx];
        }
    }

    // --- UTILS ---
    // funckja przelaczajaca aktywny panel
    private void ShowPanel(GameObject panelToShow)
    {
        // Wy³¹czamy wszystkie mo¿liwe panele
        mainMenuPanel.SetActive(false);
        modeSelectionPanel.SetActive(false);
        gameplaySetupPanel.SetActive(false);

        // --- NOWE: Wy³¹czamy te¿ panel ustawieñ
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // W³¹czamy ten jeden wybrany
        panelToShow.SetActive(true);
    }

    private int GetNextValidIndex(int currentIndex, int direction, int maxCount, int occupiedIndex)
    {
        // 1. Obliczamy wstêpny nowy indeks (z zawijaniem pêtli modulo)
        int nextIndex = (currentIndex + direction + maxCount) % maxCount;

        // 2. SPRAWDZENIE KOLIZJI: Czy ten indeks jest zajêty przez drugiego gracza?
        if (nextIndex == occupiedIndex)
        {
            // Jeœli tak, przeskakujemy o jeszcze jedno pole w tym samym kierunku
            // 
            nextIndex = (nextIndex + direction + maxCount) % maxCount;

            // UWAGA: Jeœli masz tylko 2 kolory w grze, ta logika sprawi, ¿e 
            // gracz "odbije siê" od zajêtego pola i wróci na swoje miejsce. 
            // To poprawne zachowanie (brak wolnych miejsc do ruchu).
        }

        return nextIndex;
    }
}
