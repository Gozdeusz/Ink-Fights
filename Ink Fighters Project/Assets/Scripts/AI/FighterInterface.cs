using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Linq;

/// <summary>
/// Mózg AI dzia³aj¹cy w trybie "Inference" (Wnioskowanie).
/// <para>
/// Ten skrypt nie uczy siê. S³u¿y do wczytania gotowego pliku .json (Q-Table) 
/// i sterowania przeciwnikiem komputerowym w finalnej wersji gry (PvAI).
/// </para>
/// </summary>
public class FighterInterface : MonoBehaviour
{
    [Header("AI Configuration")]
    [Tooltip("Nazwa pliku z modelem w StreamingAssets (np. qtable_aggressive.json).")]
    public string modelFileName = "qtable_aggressive.json";

    [Tooltip("Jak czêsto AI podejmuje decyzjê (np. 0.1s = 10 razy na sekundê).")]
    public float decisionFrequency = 0.1f;

    [Tooltip("Czy w³¹czyæ AI automatycznie po starcie sceny.")]
    public bool aiActiveOnStart = false;

    [Header("References")]
    public FighterController myController;
    public FighterController enemyController;

    // Struktura danych do deserializacji JSONa
    public class ModelData
    {
        public Dictionary<string, float[]> q_table;
        public float epsilon;
        public int episodes;
    }

    private Dictionary<string, float[]> qTable;
    private float timer;
    private bool isAIActive = false;

    // --- CYKL ¯YCIA ---

    private void Start()
    {
        LoadModel();

        if (aiActiveOnStart)
        {
            SetAIActive(true);
        }
    }

    private void Update()
    {
        // Jeœli AI wy³¹czone lub brak modelu -> nic nie rób
        if (!isAIActive || qTable == null) return;

        timer += Time.deltaTime;
        if (timer >= decisionFrequency)
        {
            DecideAndAct();
            timer = 0;
        }
    }

    // --- £ADOWANIE DANYCH ---

    public void LoadModel()
    {
        string path = Path.Combine(Application.streamingAssetsPath, modelFileName);

        if (File.Exists(path))
        {
            try
            {
                string jsonContent = File.ReadAllText(path);
                ModelData data = JsonConvert.DeserializeObject<ModelData>(jsonContent);
                qTable = data.q_table;
                Debug.Log($"[AI] Wczytano model: {modelFileName}. Stanów w pamiêci: {qTable.Count}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AI] B³¹d odczytu JSON: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"[AI] Brak pliku modelu: {path}. Upewnij siê, ¿e plik jest w folderze StreamingAssets.");
        }
    }

    public void SetAIActive(bool state)
    {
        isAIActive = state;

        if (!state)
        {
            // Reset inputów przy wy³¹czeniu (¿eby nie zaci¹³ siê np. w bloku)
            myController.SetMoveInput(0);
            myController.SetBlock(false);
        }
        else
        {
            timer = decisionFrequency; // Wymuœ natychmiastow¹ pierwsz¹ decyzjê
        }
    }

    // --- MÓZG AI ---

    private void DecideAndAct()
    {
        string stateKey = GetStateKey();

        int bestAction = 0; // Domyœlnie Idle

        if (qTable.ContainsKey(stateKey))
        {
            float[] qValues = qTable[stateKey];

            float maxVal = qValues.Max();
            bestAction = System.Array.IndexOf(qValues, maxVal);
        }
        else
        {

            bestAction = 0;
        }

        // 4. Wykonaj
        ExecuteAction(bestAction);
    }


    private string GetStateKey()
    {
        // A. Dystans
        float currentDist = Vector3.Distance(transform.position, enemyController.transform.position);
        int distance_cat = 2;
        if (currentDist < 0.5f) distance_cat = 0;
        else if (currentDist < 2.0f) distance_cat = 1;
        else distance_cat = 2;

        // B. HP
        float enemyHpPct = enemyController.GetCurrentHealth() / enemyController.GetMaxHealth();
        int enemy_hp_lvl = (enemyHpPct < 0.5f) ? 0 : 1;

        float myHpPct = myController.GetCurrentHealth() / myController.GetMaxHealth();
        int my_hp_lvl = (myHpPct < 0.5f) ? 0 : 1;

        int enemy_state = 0;

        if (enemyController.IsBlocking)
        {
            enemy_state = 2;
        }
        else if (enemyController.IsAttacking())
        {
            enemy_state = 1;
        }

        // Budowanie klucza
        return $"{distance_cat}_{enemy_hp_lvl}_{my_hp_lvl}_{enemy_state}";
    }

    // --- WYKONANIE ---

    private void ExecuteAction(int actionId)
    {
        // Jeœli akcja to nie blok (ID 4), musimy zdj¹æ blok
        if (actionId != 4)
        {
            myController.SetBlock(false);
        }

        switch (actionId)
        {
            case 0: myController.SetMoveInput(0); break;      // Stój
            case 1: myController.SetMoveInput(-1); break;     // IdŸ w lewo
            case 2: myController.SetMoveInput(1); break;      // IdŸ w prawo
            case 3: myController.PerformAttack(); break;      // Atak
            case 4: myController.SetBlock(true); break;       // Blokuj
            case 5: myController.SetBlock(false); break;      // Przestañ blokowaæ
        }
    }
}