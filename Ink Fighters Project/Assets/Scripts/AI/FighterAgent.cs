using Newtonsoft.Json;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;


[RequireComponent(typeof(AgentServer))]
public class FighterAgent : MonoBehaviour
{
    [Header("References")]
    public FighterController myController; // Postac sterowana przez agenta
    public FighterController enemyController; // Przeciwnik

    [Header("RL Settings")]
    public float decisionFrequency = 0.1f; // Czestotliwosc podejmowania decyzji (0.1 - 10/s)
    private float timer;

    private float currentReward = 0; // Zmienna dla nagrody
    private bool episodeDone = false;
    private string finalResult = "ONGOING";

    private AgentServer myServer;
     
    // Statystyki
    private int hitsInStep = 0;
    private int blocksInStep = 0;


    [System.Serializable]
    public struct StatePacket
    {
        public int distance_cat;  // 0: Close, 1: Mid, 2: Far
        // USUNIÊTO: public int wall_sensor;
        public int enemy_hp_lvl;  // 0: Low (<50%), 1: High (>=50%) -- ZMIANA NA 2 STANY
        public int my_hp_lvl;     // 0: Low (<50%), 1: High (>=50%) -- ZMIANA NA 2 STANY
        public int enemy_state;   // 0: Idle, 1: Attack, 2: Block (opcjonalne)
        public float reward;
        public bool done;
        public string result;     // "WIN", "LOSS", "DRAW", "ONGOING"
        public int hits_count;
        public int blocks_count;
    }

    private void Awake() => myServer = GetComponent<AgentServer>();

    private void Update()
    {
        if (!myServer.isConnected) return;

        // Odbiór akcji
        string actionStr = myServer.GetLatestAction();
        if (!string.IsNullOrEmpty(actionStr) && int.TryParse(actionStr, out int actionId))
        {
            ExecuteAction(actionId);
        }

        // Wysy³anie stanu
        timer += Time.deltaTime;
        if (timer >= decisionFrequency)
        {
            CalculateStepRewards(); // Oblicz kary za czas/pozycjê
            SendObservation();
            timer = 0;
        }
    }

    // --- KARY ZA PASYWNOŒÆ (Co krok) ---
    private void CalculateStepRewards()
    {
        // 1. Kara za up³yw czasu (Agresja!)
        AddReward(-0.03f);


        float dist = Vector3.Distance(transform.position, enemyController.transform.position);
        if (dist > 1.0f)
        {
            AddReward(-0.1f); // By³o -0.05f, teraz -0.1f (podkrêcone)
        }

        // USUNIÊTO: Kara za œciany (kompletnie)

        // 3. Ma³a kara za blokowanie
        if (myController.IsBlocking)
        {
            AddReward(-0.1f);
        }
    }

    private void SendObservation()
    {
        StatePacket packet = new StatePacket();

        float currentDist = Vector3.Distance(transform.position, enemyController.transform.position);
        if (currentDist < 0.5f) packet.distance_cat = 0;
        else if (currentDist < 2.0f) packet.distance_cat = 1;
        else packet.distance_cat = 2;

        float enemyHpPct = enemyController.GetCurrentHealth() / enemyController.GetMaxHealth();
        packet.enemy_hp_lvl = (enemyHpPct < 0.5f) ? 0 : 1;

        float myHpPct = myController.GetCurrentHealth() / myController.GetMaxHealth();
        packet.my_hp_lvl = (myHpPct < 0.5f) ? 0 : 1;

        packet.enemy_state = 0;

        if (enemyController.IsBlocking)
        {
            packet.enemy_state = 2; // Blok
        }
        else if (enemyController.IsAttacking())
        {
            packet.enemy_state = 1; // Atak
        }

        packet.reward = currentReward;
        packet.done = episodeDone;
        packet.result = finalResult;
        packet.hits_count = hitsInStep;
        packet.blocks_count = blocksInStep;

        string json = JsonUtility.ToJson(packet);
        myServer.SendState(json);

        // Reset
        if (episodeDone) { episodeDone = false; finalResult = "ONGOING"; }
        currentReward = 0;
        hitsInStep = 0;
        blocksInStep = 0;
    }

    private void ExecuteAction(int actionId)
    {
        if (actionId != 4) myController.SetBlock(false);

        switch (actionId)
        {
            case 0: myController.SetMoveInput(0); break;
            case 1: myController.SetMoveInput(-1); break;
            case 2: myController.SetMoveInput(1); break;
            case 3:
                myController.PerformAttack();
                AddReward(-1.0f);
                break;
            case 4: myController.SetBlock(true); break;
            case 5: myController.SetBlock(false); break;
        }
    }

    public void AddReward(float amount)
    {
        currentReward += amount;
    }

    public void EndEpisode(float finalReward)
    {
        currentReward += finalReward;
        episodeDone = true;

        // Ustalenie wyniku dla Pythona
        if (finalReward > 120) finalResult = "WIN";
        else if (finalReward < -120) finalResult = "LOSS";
        else finalResult = "DRAW";

        SendObservation();
    }

    public void LogHit() => hitsInStep++;
    public void LogBlock() => blocksInStep++;
}