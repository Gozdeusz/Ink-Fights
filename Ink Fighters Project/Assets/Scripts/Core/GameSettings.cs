using UnityEngine;
public enum GameMode { PvP, PvAI }
public enum AIDifficulty { Normal, Hard }
public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    public GameMode CurrentMode = GameMode.PvAI;
    public AIDifficulty CPUDifficulty = AIDifficulty.Normal;

    public int P1ColorIndex = 0;
    public int P2ColorIndex = 0;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
