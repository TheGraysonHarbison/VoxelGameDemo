using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SUI_UIUpdateRequest : MonoBehaviour
{
    public void UpdateUI()
    {
        SettingsUI.singleton.UpdateToCurrent();
    }
}
