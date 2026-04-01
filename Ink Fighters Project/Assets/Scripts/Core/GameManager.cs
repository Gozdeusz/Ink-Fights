using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // --- EVENTS DO UI ---
    public static event Action<float> OnTimerUpdate; // float: czas
    public static event Action<float, float> OnHealthUpdate; // float, float: % HP P1 i P2
    public static event Action<string> OnInfoMessage; // string: napis (3,2,1, KO)
    public static event Action<int, int> OnRoundEnd; // int, int: wygrane P1, P2
    public static event Action<string> OnMatchEnd; // string: kto wygra³ mecz

    public GameObject arenaBoundaries;

    public static event Action<bool> OnPauseToggle;
    public bool IsPaused { get; private set; } = false;
    public bool CanPause { get; private set; } = true;

    [Header("AI Training Settings")]
    public bool trainingMode = true;

    [Header("References")]
    public FighterController player1;
    public FighterController player2;
    public FighterInterface playerAI;

    [Header("Spawn Points")]
    public Transform p1Spawn;
    public Transform p2Spawn;

    [Header("Game Rules")]
    public float roundTime = 60f;
    public float postKoDelay = 2.0f;
    public float nextRoundDelay = 2.0f;
    private float currentTimer;
    public bool isRoundActive = false;

    [Header("Training Settings")]
    [Range(1f, 10f)]
    public float timeScale = 1.0f;

    [Header("Slow Motion Settings")]
    public float koSlowMoDuration = 5.0f; //Ile sekund trwa efekt slow motion przy KO
    public float koSlowMoStartScale = 0.01f; //Wartoœæ TimeScale na pocz¹tku KO
    private bool isHandlingHitStop = false; // Flaga, czy trwa w³aœnie HitStop

    private int p1Wins = 0;
    private int p2Wins = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Time.timeScale = timeScale;
        // [AI]Staly krok dla fizyki zabezpiecza obiekty przed Tunnelingiem przy przyspieszeniu symulacji
        Time.fixedDeltaTime = 0.02f;

        if (playerAI == null && player2 != null)
            playerAI = player2.GetComponent<FighterInterface>();

        var p2Input = player2.GetComponent<Player2Input>();
        if (p2Input == null) p2Input = player2.gameObject.AddComponent<Player2Input>();

        p2Input.enabled = false;

        // [AI]Optymalizacja przy przyspieszeniu symulacji (Ustawienie braku limitu FPS i wylaczneie VSync)
        if (timeScale > 1.0f)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }

        if (!trainingMode && !IsSceneLoaded("UI"))
        {
            SceneManager.LoadSceneAsync("UI", LoadSceneMode.Additive);
        }

        if (GameSettings.Instance != null)
        {

            player1.SetColor(GameSettings.Instance.P1ColorIndex);
            player2.SetColor(GameSettings.Instance.P2ColorIndex);


            if (GameSettings.Instance.CurrentMode == GameMode.PvP)
            {

                if (playerAI != null) playerAI.enabled = false;


                p2Input.enabled = true;
            }
            else 
            {

                p2Input.enabled = false;

                if (playerAI != null)
                {
                    playerAI.enabled = true;
                    string modelName = (GameSettings.Instance.CPUDifficulty == AIDifficulty.Normal) ? "qtable.json" : "qtable.json";
                    playerAI.modelFileName = modelName;
                    playerAI.LoadModel();
                }
            }
        }



        // [AI]Wylaczenie odliczanie
        if (trainingMode)
        {

            StartRoundInstant();
        }
        else
        {

            if (UILoadingScreen.Instance != null)
            {

                Debug.Log("GameManager: Czekam na UILoadingScreen...");
            }
            else
            {

                Debug.LogWarning("GameManager: Brak UILoadingScreen. Startujê natychmiast.");
                StartGameSequence();
            }
        }
    }

    public void TogglePauseGame()
    {
        if (!CanPause) return;
        IsPaused = !IsPaused;

        if (IsPaused)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }


        OnPauseToggle?.Invoke(IsPaused);
    }
    public void DisablePauseForMatchEnd()
    {
        CanPause = false;


        if (IsPaused)
        {
            TogglePauseGame();
        }
    }


    private void StartGameSequence()
    {
        if (!trainingMode)
        {
            StartCoroutine(GameLoopHuman());
        }
    }

    private void Update()
    {
        if (isRoundActive)
        {
            currentTimer -= Time.deltaTime;
            OnTimerUpdate?.Invoke(currentTimer);

            float p1HpPct = player1.GetCurrentHealth() / player1.GetMaxHealth();
            float p2HpPct = player2.GetCurrentHealth() / player2.GetMaxHealth();
            OnHealthUpdate?.Invoke(p1HpPct, p2HpPct);


            if (currentTimer <= 0)
            {
                EndRound(null);
            }

            if (!trainingMode && Time.frameCount % 60 == 0)
            {
                Debug.Log($"HP: Banana={player1.GetCurrentHealth():F0} | Red={player2.GetCurrentHealth():F0}");
            }
        }
    }


    public void StartRoundInstant()
    {
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.ShowBattleView();
        }
        if (arenaBoundaries != null) arenaBoundaries.SetActive(true);


        isRoundActive = true;
        currentTimer = roundTime;

        Time.timeScale = timeScale;


        if (player1.rb != null)
        {
            player1.rb.linearVelocity = Vector3.zero;
            player1.rb.angularVelocity = Vector3.zero;
            player1.rb.Sleep();
        }

        if (player2.rb != null)
        {
            player2.rb.linearVelocity = Vector3.zero;
            player2.rb.angularVelocity = Vector3.zero;
            player2.rb.Sleep();
        }


        player1.transform.position = p1Spawn.position;
        player1.transform.rotation = p1Spawn.rotation;

        player2.transform.position = p2Spawn.position;
        player2.transform.rotation = p2Spawn.rotation;


        Physics.SyncTransforms();


        if (player1.rb != null) player1.rb.WakeUp();
        if (player2.rb != null) player2.rb.WakeUp();


        player1.ResetAnimator();
        player2.ResetAnimator();
        ResetHealth();
        player1.SetCanMove(true);
        player2.SetCanMove(true);
    }

    private IEnumerator GameLoopHuman()
    {
        CanPause = true;
        if (arenaBoundaries != null) arenaBoundaries.SetActive(true);
        ResetPositions();
        ResetHealth();
        OnHealthUpdate?.Invoke(1f, 1f);

        player1.SetCanMove(false);
        player2.SetCanMove(false);
        if (playerAI != null) playerAI.SetAIActive(false);

        OnInfoMessage?.Invoke("3");
        yield return new WaitForSeconds(1.0f);
        OnInfoMessage?.Invoke("2");
        yield return new WaitForSeconds(1.0f);
        OnInfoMessage?.Invoke("1");
        yield return new WaitForSeconds(1.0f);
        OnInfoMessage?.Invoke("FIGHT!");
        yield return new WaitForSeconds(1.0f);
        OnInfoMessage?.Invoke("");

        isRoundActive = true;
        currentTimer = roundTime;
        player1.SetCanMove(true);
        player2.SetCanMove(true);
        if (playerAI != null) playerAI.SetAIActive(true);
    }


    public void ResetPositions()
    {
        player1.transform.position = p1Spawn.position;
        player1.transform.rotation = p1Spawn.rotation;
        player1.rb.linearVelocity = Vector3.zero;

        player2.transform.position = p2Spawn.position;
        player2.transform.rotation = p2Spawn.rotation;
        player2.rb.linearVelocity = Vector3.zero;

        player1.ResetAnimator();
        player2.ResetAnimator();
    }


    private void ResetHealth()
    {
        player1.ResetHealth();
        player2.ResetHealth();
    }

    public void DoHitStop(float duration)
    {
        if (isHandlingHitStop) return;
        StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        isHandlingHitStop = true;
        float originalScale = Time.timeScale;


        Time.timeScale = 0.01f;


        yield return new WaitForSecondsRealtime(duration);


        Time.timeScale = originalScale;
        isHandlingHitStop = false;
    }

    private IEnumerator KOSlowMotionSequence()
    {

        Time.timeScale = koSlowMoStartScale;

        float elapsed = 0f;


        while (elapsed < koSlowMoDuration)
        {

            elapsed += Time.unscaledDeltaTime;


            float t = elapsed / koSlowMoDuration;

            float currentScale = Mathf.Lerp(koSlowMoStartScale, 1.0f, t * t);

            Time.timeScale = currentScale;
            yield return null;
        }


        Time.timeScale = 1.0f;
    }


    public void PlayerDied(FighterController loser)
    {
        if (!isRoundActive) return;
        float p1Hp = (loser == player1) ? 0f : player1.GetCurrentHealth() / player1.GetMaxHealth();
        float p2Hp = (loser == player2) ? 0f : player2.GetCurrentHealth() / player2.GetMaxHealth();


        OnHealthUpdate?.Invoke(p1Hp, p2Hp);
        if (arenaBoundaries != null) arenaBoundaries.SetActive(false);

        isRoundActive = false;
        player1.SetCanMove(false);
        player2.SetCanMove(false);
        if (playerAI != null) playerAI.SetAIActive(false);
        FighterController winner = (loser == player1) ? player2 : player1;
        if (trainingMode)
        {
            EndRound(winner);
        }
        else
        {

            StartCoroutine(DeathSequence(winner));
        }
    }

    private IEnumerator DeathSequence(FighterController winner)
    {
        yield return new WaitForSecondsRealtime(0.2f);
        StartCoroutine(KOSlowMotionSequence());

        yield return new WaitForSeconds(postKoDelay);


        EndRound(winner);
    }


    private void EndRound(FighterController winner)
    {
        isRoundActive = false;

        Time.timeScale = timeScale;

        if (!trainingMode)
        {
            if (playerAI != null) 
                playerAI.SetAIActive(false);
        }

        // [AI]Nagroda i restart
        if (trainingMode)
        {
            // [AI]Remis
            if (winner == null)
            {
                if (player1.GetComponent<FighterAgent>()) player1.GetComponent<FighterAgent>().EndEpisode(-100f);
                if (player2.GetComponent<FighterAgent>()) player2.GetComponent<FighterAgent>().EndEpisode(-100f);
            }
            // [AI]Konkretny zwyciezca
            else
            {
                // [AI]Nagroda za zwyciezstwo
                var winnerAgent = winner.GetComponent<FighterAgent>();
                if (winnerAgent) winnerAgent.EndEpisode(200f);

                // [AI]Kara za przegrana
                FighterController loser = (winner == player1) ? player2 : player1;
                var loserAgent = loser.GetComponent<FighterAgent>();
                if (loserAgent) loserAgent.EndEpisode(-160f);
            }

            // [AI]Natychmiastowy restart
            StartRoundInstant();
            return;
        }


        if (winner == player2) p2Wins++;
        else if (winner == player1) p1Wins++;


        OnRoundEnd?.Invoke(p1Wins, p2Wins);

        string msg = winner == null ? "DRAW" : (winner == player1 ? "P1 WINS" : "P2 WINS");
        OnInfoMessage?.Invoke(msg);

        if (p1Wins >= 2 || p2Wins >= 2)
        {
            string winnerName = p1Wins > p2Wins ? "Player 1" : "Player 2";
            if (p1Wins > p2Wins && playerAI != null && playerAI.enabled) winnerName = "Player";
            else if (p2Wins > p1Wins && playerAI != null && playerAI.enabled) winnerName = "CPU";

            if (CameraManager.Instance != null)
            {

                CameraManager.Instance.FocusOnWinner(p1Wins > p2Wins);
            }

            OnMatchEnd?.Invoke(winnerName);
        }
        else
        {
            StartCoroutine(NextRoundRoutine());
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


    private IEnumerator NextRoundRoutine()
    {
        yield return new WaitForSeconds(nextRoundDelay);
        StartCoroutine(GameLoopHuman());
    }

    private void OnEnable()
    {

        UILoadingScreen.OnLoadingFinished += StartGameSequence;
    }

    private void OnDisable()
    {

        UILoadingScreen.OnLoadingFinished -= StartGameSequence;
    }
}