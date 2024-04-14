using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameplayManager.UpdateSettings += UpdateFog;
        UpdateFog();
    }

    void UpdateFog()
    {
        RenderSettings.fog = SettingsManager.UseFog;

        //Ratios
        int scale = SettingsManager.LOD;
        int min = 16 * (scale - 1);
        int max = min + 16;

        min += 8;

        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = min;
        RenderSettings.fogEndDistance = max;
    }

    private void OnDestroy()
    {
        GameplayManager.UpdateSettings -= UpdateFog;
    }
}
