using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class UI_AlertBox : MonoBehaviour
{
    public TextMeshProUGUI labelText;
    private UnityEvent calledEvent;

    public void Setup(string label, UnityEvent calledEvent)
    {
        labelText.text = label;
        this.calledEvent = calledEvent;
    }

    public void Confirm()
    {
        calledEvent.Invoke();
        UI_Manager.current.DestroyAlertBox();
    }

    public void Deny()
    {
        UI_Manager.current.DestroyAlertBox();
    }
}
