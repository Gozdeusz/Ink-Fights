using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class UIGame : MonoBehaviour
{
    [Header("HUD References")]
    [SerializeField] private Image p1HealthBar;
    [SerializeField] private Image p2HealthBar;
    [SerializeField] private TMP_Text timerText;

    [SerializeField] private Image p1PortraitImage; // Obiekt UI Image dla Gracza 1
    [SerializeField] private Image p2PortraitImage; // Obiekt UI Image dla Gracza 2
    [SerializeField] private Sprite[] portraitIcons;

    [SerializeField] private GameObject pausePanel;

    [Header("Wins Icons")]
    [SerializeField] private GameObject[] p1WinDots; // Tablica 2 obrazków (wype³nionych)
    [SerializeField] private GameObject[] p2WinDots;
    [SerializeField] private Color winColor;
    private Color emptyColor = Color.white;

    [Header("Info Overlay")]
    [SerializeField] private TMP_Text infoText; // Odliczanie, KO

    [Header("End Screen")]
    [SerializeField] private GameObject endMatchPanel;
    [SerializeField] private TMP_Text endMatchResultText;

    [SerializeField] private RectTransform endMatchPanelRect;

    private void Start()
    {
    }

    private void OnEnable()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        ResetGameUI();

        SetupPortraits();
        GameManager.OnPauseToggle += HandlePauseToggle;



        // Subskrypcja eventów z GameManagera
        GameManager.OnTimerUpdate += UpdateTimer;
        GameManager.OnHealthUpdate += UpdateHealthBars;
        GameManager.OnInfoMessage += ShowInfoMessage;
        GameManager.OnRoundEnd += UpdateWinDots;
        GameManager.OnMatchEnd += ShowEndScreen;


    }

    private void SetupPortraits()
    {
        // Sprawdzamy czy mamy ustawienia i czy przypisano listê ikonek
        if (GameSettings.Instance != null && portraitIcons != null && portraitIcons.Length > 0)
        {
            // Pobieramy indeksy wybrane w Menu
            int p1Idx = GameSettings.Instance.P1ColorIndex;
            int p2Idx = GameSettings.Instance.P2ColorIndex;

            // Ustawiamy portret P1 (zabezpieczenie przed wyjœciem poza tablicê)
            if (p1PortraitImage != null && p1Idx >= 0 && p1Idx < portraitIcons.Length)
            {
                p1PortraitImage.sprite = portraitIcons[p1Idx];
            }

            // Ustawiamy portret P2
            if (p2PortraitImage != null && p2Idx >= 0 && p2Idx < portraitIcons.Length)
            {
                p2PortraitImage.sprite = portraitIcons[p2Idx];
            }
        }
    }

    private void ResetGameUI()
    {
        // Ukrywamy panel koñca gry
        endMatchPanel.SetActive(false);

        // Czyœcimy teksty
        infoText.text = "";

        // Resetujemy kropki zwyciêstw
        UpdateWinDots(0, 0);

        // Resetujemy paski ¿ycia na 100% (wizualnie)
        UpdateHealthBars(1f, 1f);
    }

    private void OnDisable()
    {
        // Odsubskrybowanie (Wa¿ne, by unikn¹æ b³êdów przy zmianie scen)
        GameManager.OnTimerUpdate -= UpdateTimer;
        GameManager.OnHealthUpdate -= UpdateHealthBars;
        GameManager.OnInfoMessage -= ShowInfoMessage;
        GameManager.OnRoundEnd -= UpdateWinDots;
        GameManager.OnMatchEnd -= ShowEndScreen;
        GameManager.OnPauseToggle -= HandlePauseToggle;
    }

    private void HandlePauseToggle(bool isPaused)
    {
        if (pausePanel != null)
        {
            // SetActive(true) automatycznie odpali animacjê podpiêt¹ pod Entry w Animatorze
            pausePanel.SetActive(isPaused);
        }
    }

    public void OnClick_ResumeGame()
    {
        // Po prostu wo³amy to samo co przycisk Esc - odwracamy stan pauzy
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePauseGame();
        }
    }

    public void OnClick_ExitToMainMenu()
    {
        // BARDZO WA¯NE: Musimy odblokowaæ czas przed zmian¹ sceny, 
        // inaczej Menu G³ówne bêdzie zamro¿one!
        Time.timeScale = 1f;

        if (UILoadingScreen.Instance != null)
        {
            UILoadingScreen.Instance.LoadScene("MainMenuScene");
        }
        else
        {
            SceneManager.LoadScene("MainMenuScene");
        }
    }

    // --- OBS£UGA WYDARZEÑ ---

    private void UpdateTimer(float time)
    {
        timerText.text = Mathf.CeilToInt(time).ToString();
    }

    private void UpdateHealthBars(float p1HpPercent, float p2HpPercent)
    {
        p1HealthBar.fillAmount = p1HpPercent;
        p2HealthBar.fillAmount = p2HpPercent;
    }

    private void ShowInfoMessage(string message)
    {
        infoText.text = message;
        // Opcjonalnie: wyczyœæ tekst po chwili, jeœli GameManager tego nie robi
        if (message == "") infoText.text = "";
    }

    private void UpdateWinDots(int p1Wins, int p2Wins)
    {
        // Obs³uga Gracza 1
        for (int i = 0; i < p1WinDots.Length; i++)
        {
            // Upewniamy siê, ¿e obiekt jest w³¹czony (¿eby by³o widaæ pusty slot)
            p1WinDots[i].SetActive(true);

            // Pobieramy Image i zmieniamy kolor
            Image dotImage = p1WinDots[i].GetComponent<Image>();
            if (dotImage != null)
            {
                // Jeœli numer kropki (i) jest mniejszy ni¿ liczba wygranych -> Kolor Wygranej
                // W przeciwnym razie -> Kolor Pusty
                dotImage.color = (i < p1Wins) ? winColor : emptyColor;
            }
        }

        // Obs³uga Gracza 2
        for (int i = 0; i < p2WinDots.Length; i++)
        {
            p2WinDots[i].SetActive(true);

            Image dotImage = p2WinDots[i].GetComponent<Image>();
            if (dotImage != null)
            {
                dotImage.color = (i < p2Wins) ? winColor : emptyColor;
            }
        }
    }

    private void ShowEndScreen(string winnerName)
    {
        infoText.text = "";
        endMatchPanel.SetActive(true);
        endMatchResultText.text = $"{winnerName} Wins!";

        // --- NOWE: Zmiana pozycji panelu w zale¿noœci od zwyciêzcy ---
        if (endMatchPanelRect != null)
        {
            Vector2 newPos = endMatchPanelRect.anchoredPosition;

            // Sprawdzamy czy wygra³ Gracz 1 ("Player" lub "Player 1")
            // Jeœli tak -> Przesuwamy panel w PRAWO (450), bo kamera jest na graczu po lewej
            if (winnerName == "Player 1" || winnerName == "Player")
            {
                newPos.x = 450f;
            }
            // W przeciwnym razie (CPU / Player 2) -> Panel w LEWO (-450)
            else
            {
                newPos.x = -450f;
            }

            endMatchPanelRect.anchoredPosition = newPos;
        }
    }

    // --- BUTTONS ---

    public void OnClick_ReturnToMenu()
    {
        if (UILoadingScreen.Instance != null)
        {
            UILoadingScreen.Instance.LoadScene("MainMenuScene");
        }
        else
        {
            SceneManager.LoadScene("MainMenuScene");
        }
    }

}
