using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    //Sudo Singleton
    public static SettingsUI singleton;
    void Awake()
    {
        singleton = this;
    }

    //Variables
    public Slider LOD_slider;
    public TextMeshProUGUI LOD_Label;
    [Space]
    public Slider mouse_slider;
    public TextMeshProUGUI mouse_Label;
    [Space]
    public Toggle fullscreen_toggle;
    public Toggle fog_toggle;

    //Update UI To Current Settings
    public void UpdateToCurrent()
    {
        LOD_slider.value = SettingsManager.LOD;
        mouse_slider.value = SettingsManager.MouseSensitivity;
        fullscreen_toggle.isOn = SettingsManager.UseFullscreen;
        fog_toggle.isOn = SettingsManager.UseFog;

        LOD_Label.text = "Loading Distance: " + SettingsManager.LOD.ToString();
        mouse_Label.text = "Mouse Sensitivity: " + SettingsManager.MouseSensitivity.ToString();
    }

    //Set Labels
    private void Update()
    {
        LOD_Label.text = "Loading Distance: " + LOD_slider.value.ToString();
        mouse_Label.text = "Mouse Sensitivity: " + mouse_slider.value.ToString();
    }

    //Apply Settings
    public void ApplySettings()
    {
        SettingsManager.LOD = Mathf.RoundToInt(LOD_slider.value);
        SettingsManager.MouseSensitivity = Mathf.RoundToInt(mouse_slider.value);
        SettingsManager.UseFullscreen = fullscreen_toggle.isOn;
        SettingsManager.UseFog = fog_toggle.isOn;

        SettingsManager.SaveSettings();

        //Handle Fullscreen
        if (fullscreen_toggle.isOn)
        {
#if UNITY_STANDALONE_WIN
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
#else
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
#endif
            Screen.fullScreen = true;
        }
        else
        {
            Screen.fullScreen = false;
        }

        //Handle Events
        GameplayManager.singleton.SettingsUpdated();
    }
}
