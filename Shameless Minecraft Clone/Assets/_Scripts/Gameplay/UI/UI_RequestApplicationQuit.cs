using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_RequestApplicationQuit : MonoBehaviour
{
    public void QuitRequest()
    {
        GameplayManager.singleton.QuitGame();
    }
}
