using UnityEngine;

/// <summary>
/// Wykrywa trafienia od <see cref="Hitbox"/>.
/// </summary>
public class Hurtbox : MonoBehaviour
{
    /// <summary>
    /// Referencja do g³ównego skryptu posiadaj¹cego ten Hurtbox.
    /// </summary>
    [SerializeField] private MonoBehaviour ownerBehavior;

    /// <summary>
    /// Zcache'owana referencja do interfejsu IDamageable w³aœciciela.
    /// </summary>
    private IDamageable damageableObject;

    // Pobiera interfejs z przypisanego komponentu
    private void Awake()
    {
        damageableObject = ownerBehavior as IDamageable;

        if (damageableObject == null)
        {
            Debug.LogError("Hurtbox: Przypisany owner nie implementuje IDamageable!");
        }
    }

    /// <summary>
    /// Event wywolany podczas kolizji. Przekazuje obrazenia do wlaciciela. 
    /// </summary>
    /// <param name="damage">Wartosc zadanych obrazen</param>
    /// <returns>
    /// <c>true</c> - atak zostal zablokowany
    /// <c>false</c> - zadano wszytskie obrazenia
    /// </returns>
    public bool OnHit(float damage)
    {
        if (damageableObject != null)
        {
            return damageableObject.TakeDamage(damage);
        }

        // Jesli nie znaleziono ownera zakladamy brak bloku
        return false;
    }

    /// <summary>
    /// Zwraca wlasciciela Hurtboxa.
    /// Zastosowane do sprawdzania, czy nie atakujemy sami siebie.
    /// </summary>
    public GameObject GetOwnerObject()
    {
        return ownerBehavior.gameObject;
    }
}
