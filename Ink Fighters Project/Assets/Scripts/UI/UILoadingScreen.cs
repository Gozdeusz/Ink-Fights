using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class UILoadingScreen : MonoBehaviour
{
    public static UILoadingScreen Instance;

    [Header("References")]
    [SerializeField] private GameObject loadingPanel; // To ten sam obiekt, na którym jest skrypt
    [SerializeField] private Animator panelAnimator;

    [Header("Settings")]
    [SerializeField] private float postLoadDelay = 0.5f;

    public static event Action OnLoadingFinished;
    private bool isAnimationFinished = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // ...to znaczy, ¿e jestem tym "nowym", który zaraz zostanie zniszczony przez GlobalUIManager.
            // NIE NADPISUJ Instance! SiedŸ cicho i czekaj na zniszczenie.
            return;
        }
        // Prosty Singleton - zak³adamy, ¿e tylko jeden MainCanvas istnieje
        Instance = this;

        // Upewniamy siê, ¿e na starcie panel jest aktywny (¿eby móg³ obs³u¿yæ logikê),
        // ale Animator w stanie "Hidden" sprawi, ¿e bêdzie niewidoczny.
        loadingPanel.SetActive(true);
    }

    // --- Animation Event ---
    public void OnAnimationEnd()
    {
        isAnimationFinished = true;
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isAnimationFinished = false;
        panelAnimator.SetTrigger("Open");

        yield return new WaitUntil(() => isAnimationFinished);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            yield return null;
        }

        // Wa¿ne: Po za³adowaniu sceny GlobalUIManager prze³¹czy kontenery (Menu -> HUD),
        // ale Loading Screen jest "nad nimi", wiêc nadal zas³ania ekran.

        isAnimationFinished = false;
        panelAnimator.SetTrigger("Close");

        yield return new WaitUntil(() => isAnimationFinished);

        if (postLoadDelay > 0)
        {
            yield return new WaitForSeconds(postLoadDelay);
        }

        OnLoadingFinished?.Invoke();
    }
}

