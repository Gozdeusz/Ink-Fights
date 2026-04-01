using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    [Header("Cameras")]
    [SerializeField] private CinemachineCamera battleCamera; // G³ówna kamera (Target Group)
    [SerializeField] private CinemachineCamera p1WinCamera;  // Kamera na P1
    [SerializeField] private CinemachineCamera p2WinCamera;  // Kamera na P2

    private void Awake()
    {
        // Prosty Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Wraca do widoku walki (resetuje priorytety zwyciêzców)
    /// </summary>
    public void ShowBattleView()
    {
        // Ustawiamy bazow¹ kamerê jako g³ówn¹
        battleCamera.Priority = 10;

        // Kamery zwyciêzców id¹ w odstawkê
        p1WinCamera.Priority = 0;
        p2WinCamera.Priority = 0;
    }

    /// <summary>
    /// Prze³¹cza na kamerê zwyciêzcy (p³ynne przejœcie)
    /// </summary>
    public void FocusOnWinner(bool isPlayer1)
    {
        // Zwyciêska kamera dostaje wy¿szy priorytet (20) ni¿ BattleCamera (10)
        // Dziêki temu Cinemachine Brain p³ynnie przejedzie do nowego widoku.

        if (isPlayer1)
        {
            p1WinCamera.Priority = 20;
            p2WinCamera.Priority = 0;
        }
        else
        {
            p1WinCamera.Priority = 0;
            p2WinCamera.Priority = 20;
        }
    }
}
