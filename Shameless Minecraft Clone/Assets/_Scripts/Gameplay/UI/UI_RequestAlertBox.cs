using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class UI_RequestAlertBox : MonoBehaviour
{
    public string RequestLabel = "Label";
    public UnityEvent calledEvent;

    public void AlertRequest()
    {
        UI_Manager.current.SpawnAlertBox(RequestLabel, calledEvent);
    }
}
