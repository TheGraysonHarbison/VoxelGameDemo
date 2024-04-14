using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class SettingsManager
{
    public static int LOD;
    public static int MouseSensitivity;
    public static bool UseFullscreen;
    public static bool UseFog;

    private const string PATH = "/Settings.json";

    //Init
    public static void Init()
    {
        //Check For File
        if(HasSettingsFile())
        {
            LoadSettings();
        }
        else // Set Defaults
        {
            Reset();
        }
    }

    //Check For Settings
    private static bool HasSettingsFile()
    {
        string fullPath = Application.persistentDataPath + PATH;
        return File.Exists(fullPath);
    }

    //Get Default Settings
    public static void SetDefaults()
    {
        LOD = 3;
        MouseSensitivity = 6;
        UseFog = true;

#if UNITY_WEBGL
        UseFullscreen = false;
#else
        UseFullscreen = true;
#endif

        Debug.Log("Default Settings Applied");
    }

    //Save
    public static void SaveSettings()
    {
        //Convert To JSON
        SettingsData data = new SettingsData()
        {
            LOD = LOD,
            MouseSensitivity = MouseSensitivity,
            UseFullscreen = UseFullscreen,
            UseFog = UseFog
        };
        string json = JsonUtility.ToJson(data);

        //Write
        string fullPath = Application.persistentDataPath + PATH;
        File.WriteAllText(fullPath, json);

        Debug.Log("Settings File Saved To " + fullPath);
    }

    //Load
    public static void LoadSettings()
    {
        //Read Json
        string fullPath = Application.persistentDataPath + PATH;
        string json = File.ReadAllText(fullPath);

        SettingsData data = JsonUtility.FromJson<SettingsData>(json);

        //Apply
        LOD = data.LOD;
        MouseSensitivity = data.MouseSensitivity;
        UseFullscreen = data.UseFullscreen;
        UseFog = data.UseFog;

        Debug.Log("Settings Loaded From " + fullPath);
    }

    //Reset
    public static void Reset()
    {
        SetDefaults();
        SaveSettings();
    }

    //Update
    public static void UpdateSettings(int lod, int ms, bool fs, bool f)
    {
        //Set Variables
        LOD = lod;
        MouseSensitivity = ms;
        UseFullscreen = fs;
        UseFog = f;
    }
}

[System.Serializable]
public struct SettingsData
{
    public int LOD;
    public int MouseSensitivity;
    public bool UseFullscreen;
    public bool UseFog;
}