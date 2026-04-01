using UnityEngine;
using System.Collections;

public class TestBag : MonoBehaviour, IDamageable
{
    [SerializeField] private Renderer myRenderer;
    private Color originalColor;

    private void Start()
    {
        if (myRenderer != null) originalColor = myRenderer.material.color;
    }

    public bool TakeDamage(float amount)
    {
        Debug.Log($"<color=yellow>Worek: Otrzymano {amount} obra¿eñ!</color>");

        if (myRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(FlashRed());
        }

        // Worek nie blokuje, wiêc zwracamy false (cios nie zosta³ zablokowany)
        return false;
    }

    private IEnumerator FlashRed()
    {
        myRenderer.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        myRenderer.material.color = originalColor;
    }
}
