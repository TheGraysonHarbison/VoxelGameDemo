using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class UI_UpdateVariable : MonoBehaviour
{
    public enum UpdateType
    {
        Seed,
    }

    [Header("Update Type")]
    public UpdateType type;

    //Update Trigger
    public void OnTextUpdate(string text)
    {
        switch(type)
        {
            case UpdateType.Seed:
                UpdateSeed(text);
                break;
        }
    }

    //Update Function
    private void UpdateSeed(string text)
    {
        if(text.Length > 0)
            GameplayManager.singleton.SetSeed(text.GetHashCode());
        else
            GameplayManager.singleton.SetSeed(Random.Range(-10000, 10000));
    }
}
