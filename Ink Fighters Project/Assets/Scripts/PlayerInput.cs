using UnityEditor.XR;
using UnityEngine;

/// <summary>
/// Odpowiada za sterowanie dostepne dla gracza
/// </summary>
public class PlayerInput : MonoBehaviour
{
    private FighterController fighterController;
    private GameControls controls;

    private void Awake()
    {
        fighterController = GetComponent<FighterController>();
        controls = new GameControls();

        // Ruch lewo - prawo
        controls.Player1.Move.performed += ctx => fighterController.SetMoveInput(ctx.ReadValue<float>());
        controls.Player1.Move.canceled += ctx => fighterController.SetMoveInput(0f);

        // Atak
        controls.Player1.Attack.performed += ctx => fighterController.PerformAttack();

        // Blok
        controls.Player1.Block.performed += ctx => fighterController.SetBlock(true);
        controls.Player1.Block.canceled += ctx => fighterController.SetBlock(false);
    }

    private void OnEnable() => controls.Player1.Enable();
    private void OnDisable() => controls.Player1.Disable();
}
