using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_RequestSceneChange : MonoBehaviour
{
    public string SceneName;
    public void SceneRequest()
    {
        GameplayManager.singleton.ChangeScene(SceneName);
    }
}
