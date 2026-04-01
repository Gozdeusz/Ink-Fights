using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Potrzebne do obs³ugi zdarzeñ myszy
using TMPro; // Potrzebne do TextMeshPro

[RequireComponent(typeof(Image))]
public class UIButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private GameObject splashImageObject; // Obiekt z obrazkiem splasha
    [SerializeField] private TextMeshProUGUI buttonText;   // Tekst na guziku

    [Header("Text Settings")]
    [SerializeField] private Color normalTextColor = Color.black; // Kolor zwyk³y (np. czarny)
    [SerializeField] private Color pressedTextColor = Color.white; // Kolor po wciœniêciu (np. bia³y)

    private void Start()
    {
        // Na starcie resetujemy wygl¹d do stanu spoczynku
        ResetVisuals();
    }

    // Wywo³ywane w momencie wciœniêcia przycisku
    public void OnPointerDown(PointerEventData eventData)
    {
        // 2. Zmieñ kolor tekstu
        if (buttonText != null)
            buttonText.color = pressedTextColor;
        // 1. Poka¿ Splash
        if (splashImageObject != null)
            splashImageObject.SetActive(true);
    }

    // Wywo³ywane w momencie puszczenia przycisku
    public void OnPointerUp(PointerEventData eventData)
    {
        //ResetVisuals();
    }

    // Funkcja pomocnicza do resetowania stanu
    private void ResetVisuals()
    {
        // Ukryj Splash
        if (splashImageObject != null)
            splashImageObject.SetActive(false);

        // Przywróæ kolor tekstu
        if (buttonText != null)
            buttonText.color = normalTextColor;
    }

    // Zabezpieczenie: Jeœli kursor wyjedzie poza przycisk trzymaj¹c go, te¿ zresetuj
    private void OnDisable()
    {
        ResetVisuals();
    }
}
