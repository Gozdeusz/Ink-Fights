using UnityEngine;

public class TestEnemyControls : MonoBehaviour
{
    private FighterController fighterController;

    private void Awake()
    {
        fighterController = GetComponent<FighterController>();
    }

    void Update()
    {
        // --- 1. RUCH (Strza³ki Lewo/Prawo) ---
        float moveDir = 0f;
        if (Input.GetKey(KeyCode.RightArrow)) moveDir = -1f;
        if (Input.GetKey(KeyCode.LeftArrow)) moveDir = 1f;

        fighterController.SetMoveInput(moveDir);

        // --- 2. ATAK (Strza³ka w Dó³) ---
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            fighterController.PerformAttack();
        }

        // --- 3. BLOK (Strza³ka w Górê) ---
        // Przekazujemy true dopóki trzymasz klawisz
        bool isHoldingBlock = Input.GetKey(KeyCode.UpArrow);
        fighterController.SetBlock(isHoldingBlock);
    }
}