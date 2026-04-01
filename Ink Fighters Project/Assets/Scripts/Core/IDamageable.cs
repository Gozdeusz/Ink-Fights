using UnityEngine;

/// <summary>
/// Definiuje obiekty, ktore moga otrzymywac obrazenia.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Wysyla obrazenia do obiektu
    /// </summary>
    /// <param name="amount">Wartosc obrazen</param>
    /// <returns>
    /// <c>true</c> - atak zostal zablokowany
    /// <c>false</c> - zadano wszytskie obrazenia
    /// </returns>
    bool TakeDamage(float amount);
}
