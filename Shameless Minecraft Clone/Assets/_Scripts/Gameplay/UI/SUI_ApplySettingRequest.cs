using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SUI_ApplySettingRequest : MonoBehaviour
{
    public void ApplySettings()
    {
        SettingsUI.singleton.ApplySettings();
    }
}
