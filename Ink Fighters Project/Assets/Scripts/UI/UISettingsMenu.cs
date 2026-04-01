using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class UISettingsMenu : MonoBehaviour
{
    [Header("Navigation References")] // --- NOWE ---
    [SerializeField] private GameObject settingsPanelObject; // Panel, na którym jest ten skrypt (¿eby go wy³¹czyæ)
    [SerializeField] private GameObject mainMenuPanelObject;

    [Header("UI References")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle shakeToggle;

    private Resolution[] resolutions;

    private void Start()
    {
        if (settingsPanelObject == null) settingsPanelObject = gameObject;

        // --- 1. ROZDZIELCZOŒÆ (Twoja logika - jest super) ---
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            // Opcjonalnie: Filtrowanie odœwie¿ania, ¿eby nie mieæ dubli (np. 60Hz i 59Hz)
            // if (resolutions[i].refreshRateRatio.value < 59) continue; 

            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // --- 2. JAKOŒÆ (NOWE - Automatyczne nazwy) ---
        // Czyœcimy "Option A, B, C"
        qualityDropdown.ClearOptions();

        // Pobieramy nazwy z Project Settings -> Quality (np. Low, Medium, High)
        string[] qualityNames = QualitySettings.names;

        // Zamieniamy tablicê na listê dla Dropdowna
        List<string> qOptions = new List<string>(qualityNames);

        qualityDropdown.AddOptions(qOptions);

        // Ustawiamy aktualnie wybran¹ jakoœæ
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();

        // --- 3. RESZTA ---
        fullscreenToggle.isOn = Screen.fullScreen;
        shakeToggle.isOn = PlayerPrefs.GetInt("CameraShake", 1) == 1;
    }

    // --- NAVIGATION ---
    public void OnClick_ReturnToMenu()
    {
        settingsPanelObject.SetActive(false);
        if (mainMenuPanelObject != null) mainMenuPanelObject.SetActive(true);
    }

    // --- FUNKCJE USTAWIEÑ (Podepnij pod Dynamiczne eventy!) ---

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        // Czasem Unity potrzebuje odœwie¿enia rozdzielczoœci po zmianie na fullscreen,
        // ale zazwyczaj dzia³a to automatycznie.
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetCameraShake(bool isEnabled)
    {
        PlayerPrefs.SetInt("CameraShake", isEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}
