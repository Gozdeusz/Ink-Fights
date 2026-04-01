using UnityEngine;

public class Player2Input : MonoBehaviour
{
    private FighterController fighterController;
    private GameControls controls;

    private void Awake()
    {
        fighterController = GetComponent<FighterController>();
        controls = new GameControls();

        // --- RUCH  ---
        controls.Player2.Move.performed += ctx => fighterController.SetMoveInput(ctx.ReadValue<float>());
        controls.Player2.Move.canceled += ctx => fighterController.SetMoveInput(0f);

        // --- ATAK  ---
        controls.Player2.Attack.performed += ctx => fighterController.PerformAttack();

        // --- BLOK  ---
        controls.Player2.Block.performed += ctx => fighterController.SetBlock(true);
        controls.Player2.Block.canceled += ctx => fighterController.SetBlock(false);
    }

    private void OnEnable()
    {
        controls.Player2.Enable();
    }

    private void OnDisable()
    {
        controls.Player2.Disable();
    }
}
