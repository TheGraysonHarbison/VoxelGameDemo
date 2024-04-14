using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_RequestMenuChange : MonoBehaviour
{
    public int MenuIndex;
    public void MenuRequest()
    {
        UI_Manager.current.SetMenu(MenuIndex);
    }
}
