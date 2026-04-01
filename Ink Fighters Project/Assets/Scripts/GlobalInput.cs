using UnityEngine;

public class GlobalInput : MonoBehaviour
{
    private GameControls controls;

    private void Awake()
    {
        controls = new GameControls();

        // Bezpoœrednie po³¹czenie: Input -> GameManager
        // Nie potrzebujemy do tego Playera ani FighterControllera
        controls.Global.Pause.performed += ctx => TogglePause();
    }

    private void TogglePause()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePauseGame();
        }
    }

    private void OnEnable()
    {
        controls.Global.Enable();
    }

    private void OnDisable()
    {
        controls.Global.Disable();
    }
}
