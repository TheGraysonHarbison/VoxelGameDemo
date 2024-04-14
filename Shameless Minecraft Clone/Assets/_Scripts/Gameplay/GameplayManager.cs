using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayManager : MonoBehaviour
{
    //Settings
    public GameObject playerPrefab;

    //Variables
    PlayerContoller playerContoller;
    
    public enum GameState
    {
        MainMenu,
        Game
    }
    private GameState gameState = GameState.MainMenu;

    private int seed = 42069;

    //Events
    public delegate void OnSceneUnloaded(string currentScene);
    public static event OnSceneUnloaded SceneUnloaded;

    public delegate void OnSceneLoaded(string targetScene);
    public static event OnSceneLoaded SceneLoaded;

    public delegate void OnApplicationQuit();
    public static event OnApplicationQuit ApplicationQuit;

    public delegate void OnPlayerSelectsVoxel(int voxel);
    public static event OnPlayerSelectsVoxel SelectsVoxel;

    public delegate void OnSettingsUpdated();
    public static event OnSettingsUpdated UpdateSettings;


    #region Setup Functions
    //Singleton And Setup
    public static GameplayManager singleton;
    private void Awake()
    {
        if (singleton != null)
        {
            Destroy(this.gameObject);
            return;
        }

        //Setup
        singleton = this;
        DontDestroyOnLoad(gameObject);

        seed = Random.Range(0, 10000);
        gameState = GameState.MainMenu;

        SettingsManager.Init();
        SetupAtlas();
    }

    //Setup Textures
    private void SetupAtlas()
    {
        //Create Atlas
        VoxelManager.singleton.GenerateAtlas();

        /*
        if (VoxelManager.singleton.HasValidAtlas())
        {
            VoxelManager.singleton.LoadAtlas();
            GameplayManager.singleton.Application_logMessageReceived("Loaded");
        }
        else
        {
            GameplayManager.singleton.Application_logMessageReceived("Generated");
            VoxelManager.singleton.GenerateAtlas();
        }*/
    }


    //Reset Settings
    [ContextMenu("Reset Game Settings")]
    private void ResetSettings()
    {
        SettingsManager.Reset();
    }

    #endregion

    #region Scene Management

    //Scene Management
    public void ChangeScene(string sceneName)
    {
        //Run Events
        if(SceneUnloaded != null)
            SceneUnloaded.Invoke(SceneManager.GetActiveScene().name);

        //Change Scene
        SceneManager.LoadScene(sceneName);

        if (SceneLoaded != null)
            SceneLoaded.Invoke(sceneName);

        //Update State
        StartCoroutine(StateUpdate(sceneName));
    }

    private IEnumerator StateUpdate(string sceneName)
    {
        yield return 1;

        //Set State
        switch (sceneName)
        {
            case "World":
                gameState = GameState.Game;
                break;
            default:
                gameState = GameState.MainMenu;
                break;
        }

        //Apply Logic
        if (gameState == GameState.Game)
            SetupGameplay();
    }

    #endregion

    //Gameplay Management
    public void SetSeed(int s)
    {
        seed = s;
    }

    private void SetupGameplay()
    {
        SetupWorldGeneration();
        SetupPlayer();
    }

    private void SetupWorldGeneration()
    {
        //Set Settings
        WorldGenerationManager wm = WorldGenerationManager.singleton;
        wm.Seed = seed;

        //Activate
        wm.Init();
        wm.SetGenerationState(WorldGenerationManager.WorldState.Active);
    }

    private void SetupPlayer()
    {
        //Get A Valid Spawn Position
        float height = WorldGenerationManager.singleton.GetPlayerSpawnHeight();
        Vector3 spawnPoint = new Vector3(0, height, 0);

        //Spawn And Get Variables
        playerContoller = Instantiate(playerPrefab, spawnPoint, Quaternion.identity)
            .GetComponent<PlayerContoller>();
    }

    //Quit Game
    public void QuitGame()
    {
        Debug.Log("Quiting");
        if (ApplicationQuit != null)
            ApplicationQuit.Invoke();

        Application.Quit();
    }

    //Player Changes Placed Voxel
    public void CallChangedVoxel(int currentSelectedVoxel)
    {
        if (SelectsVoxel != null)
            SelectsVoxel.Invoke(currentSelectedVoxel);
    }

    //Handle Settings
    public void SettingsUpdated()
    {
        if(UpdateSettings !=  null)
            UpdateSettings.Invoke();
    }
}
