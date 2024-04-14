using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class UI_Manager : MonoBehaviour
{
    //Current Manager
    public static UI_Manager current;
    public void Awake()
    {
        current = this; 
    }

    //Settings
    [SerializeField] private GameObject alertBoxPrefab;
    [SerializeField] private Transform canvas;
    [Space]
    [SerializeField] private int startingMenuIndex = 0;
    [SerializeField] private GameObject[] menus;

    [Header("Cursor Locking")]
    public bool useCursorLock = false;
    public bool[] lockInMenus;

    [Header("Debugging")]
    public TextMeshProUGUI debugText;

    //Variables
    UI_AlertBox currentAlert;
    private int currentMenu;
    public int CurrentMenuIndex { get { return currentMenu; } }

    //UI Update
    private void Start()
    {
        SetMenu(startingMenuIndex);
    }

    public void SetMenu(int menusIndex)
    {
        //Show UI
        for (int i = 0; i < menus.Length; i++)
        {
            if(i == menusIndex)
            {
                menus[i].SetActive(true);
                currentMenu = i;
            }
            else
            {
                menus[i].SetActive(false);
            }
        }

        //Cursor Lock
        if (!useCursorLock)
        {
            Cursor.lockState = CursorLockMode.None; 
            return;
        }

        if (lockInMenus[menusIndex])
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    //Alert Box
    public void SpawnAlertBox(string label, UnityEvent calledEvent)
    {
        if(alertBoxPrefab != null)
        {
            DestroyAlertBox();
        }

        currentAlert = Instantiate(alertBoxPrefab, canvas.transform, false)
            .GetComponent<UI_AlertBox>();

        currentAlert.Setup(label, calledEvent);
    }

    public void DestroyAlertBox()
    {
        if(currentAlert != null)
        {
            Destroy(currentAlert.gameObject);
            currentAlert = null;
        }
    }

    internal void DisplayLog(string condition)
    {
        if(debugText != null) 
            debugText.text = "Notice: " + condition + "/n"; 
    }
}
