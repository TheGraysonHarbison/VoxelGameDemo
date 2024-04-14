
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;


public class WorldGenerationManager : MonoBehaviour
{
    //Singleton
    public static WorldGenerationManager singleton;
    public void Awake()
    {
        singleton = this;
    }

    //Settings
    [Header("World Settings")]
    public int Seed = 42069;
    [Space]
    public int BaseSurfaceLevel = 60;
    public WorldGenerationSettings worldGenerationSettings;

    [Header("Chunk Settings")]
    [SerializeField] private int chunkArea = 16;
    [SerializeField] private int chunkHeight = 100;

    public int ChunkArea { get { return chunkArea; } }
    public int ChunkHeight { get { return chunkHeight; } }

    [Space]
    [SerializeField] private int loadingDistance = 12;
    [SerializeField] private Material chunkMaterial;
    [SerializeField] private Material waterMaterial;

    //Variables
    public enum WorldState
    {
        Idle,
        Active,
    }

    private WorldState state = WorldState.Idle;
    public WorldState GenerationState { get { return state; } }


    [SerializeField] Vector3 playerPosition;
    [SerializeField] Vector2Int playerChunkCoords;

    NoiseSampler elevationSampler;
    NoiseSampler erosionSampler;
    NoiseSampler peaksSampler;

    NoiseSampler tempuratureSampler;
    NoiseSampler moistureSampler;

    TreeHashGrid treeGrid;

    NoiseSampler caveSampler;
    NoiseSampler maskSampler;

    //Chunks
    Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
    CancellationTokenSource tokenSource;
    CancellationToken token;

    Queue<ChunkGenerationResults> chunkGenerationResultsQueue = new Queue<ChunkGenerationResults>();
    Queue<MeshUpdateResults> meshUpdateResultsQueue = new Queue<MeshUpdateResults>();


    //Setup
    private void SetupMaterials()
    {
        if(VoxelManager.singleton == null)
        {
            Debug.Log("Voxel Manager Not Found");
            return;
        }

        //Update Material
        chunkMaterial.SetTexture("_MainTex", VoxelManager.singleton.TextureAtlas);
        waterMaterial.SetTexture("_MainTex", VoxelManager.singleton.TextureAtlas);
    }

    #region Biome Handling
    //Biome Making
    private void SetupNoiseMaps()
    {
        //Noise Sampling For World
        elevationSampler = new NoiseSampler(Seed, worldGenerationSettings.ElevationNoiseSettings);
        erosionSampler = new NoiseSampler(Seed, worldGenerationSettings.ErosionNoiseSettings);
        peaksSampler = new NoiseSampler(Seed, worldGenerationSettings.PeaksNoiseSettings);

        //Noise Sampling For Biomes
        tempuratureSampler = new NoiseSampler(Seed, worldGenerationSettings.TemperatureNoiseSettings);
        moistureSampler = new NoiseSampler(Seed, worldGenerationSettings.MoistureNoiseSettings);

        //Hash For Trees
        treeGrid = new TreeHashGrid(Seed, chunkArea, worldGenerationSettings.TreeGenerationSettings);

        //Noise Sampling For Caves
        caveSampler = new NoiseSampler(Seed, worldGenerationSettings.CaveNoiseSettings);
        maskSampler = new NoiseSampler(Seed, worldGenerationSettings.CaveMaskNoiseSettings);
    }

    public float GetWorldHeight(Vector2 point)
    {
        //Get Noises
        float elevation = Mathf.InverseLerp(-.5f, .5f, elevationSampler.Sample2D(point));
        elevation = EvaluateWorldSpline(elevation, worldGenerationSettings.ElevationSpline);

        float erosion =  Mathf.InverseLerp(-.5f, .5f, erosionSampler.Sample2D(point));
        erosion = 1 - EvaluateWorldSpline(erosion, worldGenerationSettings.ErosionSpline);

        float peaks = Mathf.InverseLerp(-.5f, .5f, peaksSampler.Sample2D(point));
        peaks = EvaluateWorldSpline(peaks, worldGenerationSettings.PeaksSpline);

        //Elevation is a base map that gets edited
        //Peaks amplifies terrain, creating dips and peaks,
        //Erosion carves mountains and flattens terrain

        //Add And Return
        float result = BaseSurfaceLevel + (100 * elevation);
        result += (100 * peaks);
        result *= erosion;

        return result;
    }

    public float EvaluateWorldSpline(float value, WorldSplinePoint[] points)
    {
        if (points.Length == 0)
            return value;

        //Less Than Curve
        if (value < points[0].time)
            return points[0].value;

        //Greater Than Curve
        if (value > points[points.Length - 1].time)
            return points[points.Length - 1].value;

        //Spline Math
        float s = value;
        for (int i = 0; i < points.Length; i++)
        {
            WorldSplinePoint a = points[i];
            WorldSplinePoint b = points[Mathf.Clamp(i+1, 0, points.Length-1)];

            //Compare
            if(value >= a.time && value <= b.time)
            {
                float t = Mathf.InverseLerp(a.time, b.time, value);
                s = Mathf.SmoothStep(a.value, b.value, t);
            }
        }

        return s;
    }

    private float DetermineHighestPoint()
    {
        return BaseSurfaceLevel * 2;
    }

    public float GetTempurature(Vector2 point)
    {
        //Evaluate Height Modifier
        float heightAtPoint = GetWorldHeight(point);
        float elevationModifier = Mathf.InverseLerp(BaseSurfaceLevel, DetermineHighestPoint(), heightAtPoint);
        elevationModifier = EvaluateWorldSpline(elevationModifier, worldGenerationSettings.TempuratureSpline);

        //Sample Tempurature
        float sample = tempuratureSampler.Sample2D(point);
        sample *= 2f;
        return sample + elevationModifier;
    }

    public float GetMoisture(Vector2 point)
    {
        //Evaluate Height Modifier
        float heightAtPoint = GetWorldHeight(point);
        float elevationModifier = Mathf.InverseLerp(BaseSurfaceLevel, DetermineHighestPoint(), heightAtPoint);
        elevationModifier = EvaluateWorldSpline(elevationModifier, worldGenerationSettings.MoistureSpline);

        //Sample Moisture
        float sample = moistureSampler.Sample2D(point);
        sample = sample *= 2f;
        return sample + elevationModifier;
    }

    public TreeHashGrid GetTreeGrid()
    {
        return treeGrid;
    }

    public float GetCaveNoise(Vector3 point)
    {
        //Sample Cave
        float c = Mathf.InverseLerp(-.5f, .5f, caveSampler.Sample3D(point));
        return c;
    }

    public float GetMaskNoise(Vector3 point)
    {
        //Sample Cave
        float c = Mathf.InverseLerp(-.5f, .5f, maskSampler.Sample3D(point));
        return c;
    }

    //Get Player Spawn
    public float GetPlayerSpawnHeight()
    {
        return Mathf.Ceil(GetWorldHeight(Vector2.zero)) + 2.3f;
    }

    #endregion

    #region Controls
    
    //Init
    public void Init()
    {
        //Test
        chunks = new Dictionary<Vector2Int, Chunk>();
        chunkGenerationResultsQueue = new Queue<ChunkGenerationResults>();
        meshUpdateResultsQueue = new Queue<MeshUpdateResults>();

        //Thread Cancelation
        tokenSource = new CancellationTokenSource();
        token = tokenSource.Token;

        //Events
        GameplayManager.SceneUnloaded += KillThreads;
        GameplayManager.UpdateSettings += UpdateLOD;

        //Noise And Materials
        UpdateLOD();

        SetupNoiseMaps();
        SetupMaterials();

        Debug.Log("World Has Been Setup");
    }

    //Set The State
    public void SetGenerationState(WorldState state)
    {
        this.state = state;
    }

    //Stop Threading
    public void KillThreads(string s)
    {
        tokenSource.Cancel();
        Debug.Log("Threads Have Been Canceled");
    }

    //Set Player Position
    public void UpdatePlayerPosition(Vector3 p)
    {
        playerPosition = p;
    }

    //Check For Valid Chunks
    public bool IsInsideLoadedChunk()
    {
        if (!chunks.ContainsKey(playerChunkCoords))
            return false;

        if (chunks[playerChunkCoords].loadingState != Chunk.LoadingState.Loaded) 
            return false;

        return true;
    }

    //Update LOD
    private void UpdateLOD()
    {
        loadingDistance = SettingsManager.LOD;
    }

    //Handle Events
    private void OnDestroy()
    {
        GameplayManager.SceneUnloaded -= KillThreads;
        GameplayManager.UpdateSettings -= UpdateLOD;
    }

    #endregion

    //World Updating
    private void Update()
    {
        //Check For Requests
        ProcessCallbacks();

        //Check For Pause
        if (state == WorldState.Idle)
            return;

        //Unload Chunks
        CheckForUnloadedChunks();

        //Update Chunk Position
        playerChunkCoords = GetChunkCoords(playerPosition);

        //Check For New Or Loadable Chunks
        for (int xCoord = -loadingDistance; xCoord < loadingDistance; xCoord++)
        {
            for (int yCoord = -loadingDistance; yCoord < loadingDistance; yCoord++)
            {
                //Find Chunks Coord
                Vector2Int targetCoord = playerChunkCoords + new Vector2Int(xCoord, yCoord);

                if(chunks.ContainsKey(targetCoord)) //Load Existing Chunk
                {
                    if (chunks[targetCoord].isCulled)
                        chunks[targetCoord].Load();
                }
                else //New Chunk
                {
                    CreateNewChunk(targetCoord);
                }
            }
        }
    }

    #region Chunk Creation, Destruction And Loading

    //Chunk Creation
    private void CreateNewChunk(Vector2Int coord)
    {
        //Get World Position
        Vector3 worldPosition = new Vector3(coord.x * chunkArea, 0, coord.y * chunkArea);

        //Give To List
        Chunk chunk = new Chunk(coord, worldPosition, chunkArea, chunkHeight, chunkMaterial, waterMaterial);
        chunks.Add(coord, chunk);
    }

    //Chunk Unloading
    private void CheckForUnloadedChunks()
    {
        foreach(Vector2Int coord in chunks.Keys)
        {
            //Check For Already Culled
            if (chunks[coord].isCulled)
                continue;

            //Check Range
            if(coord.x > playerChunkCoords.x + loadingDistance || coord.x < playerChunkCoords.x - loadingDistance)
            {
                chunks[coord].Unload();
                continue;
            }

            if (coord.y > playerChunkCoords.y + loadingDistance || coord.y < playerChunkCoords.y - loadingDistance)
            {
                chunks[coord].Unload();
                continue;
            }
        }

    }

    //Chunk Destruction
    private void RegenerateChunks()
    {
        //TODO Change This To Check Loaded Chunks And Only Regen Meshes
        Debug.Log("Regenerating");

        //Loop
        foreach (Vector2Int key in chunks.Keys)
        {
            chunks[key].loadingState = Chunk.LoadingState.WaitingOnVoxels;

            //Start Request
            RequestChunkGeneration(chunks[key]);
        }
    }

    #endregion

    #region Space Transformations
    //Get Chunk Coords
    public Vector2Int GetChunkCoords(Vector3 point)
    {
        int chunkCoordX = Mathf.FloorToInt((float)point.x / chunkArea);
        int chunkCoordY = Mathf.FloorToInt((float)point.z / chunkArea);
        return new Vector2Int(chunkCoordX, chunkCoordY);
    }

    //Convert To Block Space
    public Vector3Int VoxelLocationInWorld(Vector3 point)
    {
        Vector3Int voxelCoords = GetVoxelCoords(point);
        Vector2Int chunkCoords = GetChunkCoords(point);
        Vector3Int result = new Vector3Int(chunkCoords.x * chunkArea, 0, chunkCoords.y * chunkArea) + voxelCoords;

        return result;
    }

    //Get Voxel Coords
    public Vector3Int GetVoxelCoords(Vector3 point)
    {
        int x = Mathf.FloorToInt(point.x) % chunkArea;
        int y = Mathf.FloorToInt(point.y) % chunkHeight;
        int z = Mathf.FloorToInt(point.z) % chunkArea;

        //Check For Negative
        if (x < 0)
            x = chunkArea - Mathf.Abs(x);

        if (y < 0)
            y = chunkHeight - Mathf.Abs(y);

        if (z < 0)
            z = chunkArea - Mathf.Abs(z);

        return new Vector3Int(x, y, z);
    }

    //Voxel Checking
    public int GetVoxelAtPoint(Vector3Int samplePoint)
    {
        Vector2Int cc = GetChunkCoords(samplePoint);
        Vector3Int vc = GetVoxelCoords(samplePoint);

        //Check For Chunk
        if (!chunks.ContainsKey(cc))
        {
            return 15;
        }

        //Check For Loading
        Chunk chunk = chunks[cc];
        if (chunk.loadingState == Chunk.LoadingState.WaitingOnVoxels)
        {
            return 15;
        }


        return chunk.GetVoxel(vc) - 1;

        /*
        //Check For A Chunk
        Vector2Int chunkCoords = GetChunkCoords(ToBlockSpace(samplePoint));

        //Check For Chunk
        if (!chunks.ContainsKey(chunkCoords))
        {
            return -1;
        }

        //Check For Loading
        Chunk chunk = chunks[chunkCoords];
        if(chunk.loadingState == Chunk.LoadingState.WaitingOnVoxels)
        {
            return -1;
        }

        //Check Chunks Voxels
        Vector3Int voxelCoords = GetVoxelCoords(samplePoint);
        return chunk.GetVoxel(voxelCoords) - 1;
        */
    }

    #endregion

    #region Requests
    //Check For Callbacks
    private void ProcessCallbacks()
    {
        //Generation Callbacks
        while (chunkGenerationResultsQueue.Count > 0)
        {
            ChunkGenerationCallback(chunkGenerationResultsQueue.Dequeue());
        }

        //Mesh Callbacks
        while (meshUpdateResultsQueue.Count > 0)
        {
            MeshUpdateCallback(meshUpdateResultsQueue.Dequeue());
        }
    }

    //Generates New Voxels
    public void RequestChunkGeneration(Chunk c)
    {
        //Set State
        c.loadingState = Chunk.LoadingState.WaitingOnVoxels;

        //Create Request Data
        ChunkGenerationRequest request = new ChunkGenerationRequest()
        {
            chunkID = c.chunkID,
            area = c.area,
            height = c.height,
            position = c.position
        };

        //Create And Start Task
        Task.Factory.StartNew(() =>
        {
            ChunkGenerationResults results = VoxelMapper.CalculateNewVoxels(request);
            lock (chunkGenerationResultsQueue)
            {
                chunkGenerationResultsQueue.Enqueue(results);
            }

        }, token);
    }

    //Recive New Voxels And Start Mesh Update
    public void ChunkGenerationCallback(ChunkGenerationResults results)
    {
        //Get The Chunk
        Chunk chunk = chunks[results.chunkID];

        //Update Its Voxels
        chunk.UpdateVoxels(results.voxels, results.useTransparentPass);

        //Update The Mesh
        RequestMeshUpdate(chunk, results.voxels, results.useTransparentPass);
    }

    //Generates Mesh From Voxels
    public void RequestMeshUpdate(Chunk c, byte[,,] voxels, bool transparent)
    {
        //Set State
        c.loadingState = Chunk.LoadingState.WaitingOnMesh;

        //Create Request Data
        MeshUpdateRequest request = new MeshUpdateRequest()
        {
            chunkID = c.chunkID,
            position = c.position,
            area = c.area,
            height = c.height,
            voxelData = voxels,
            transparent = transparent
        };

        //Create And Start Task
        Task.Factory.StartNew(() =>
        {
            MeshUpdateResults results = MeshGenerator.CalculateMesh(request);
            lock (chunkGenerationResultsQueue)
            {
                meshUpdateResultsQueue.Enqueue(results);
            }

        }, token);

    }

    //Generates Mesh From Voxels
    public void MeshUpdateCallback(MeshUpdateResults results)
    {
        //Get The Chunk
        Chunk chunk = chunks[results.chunkID];

        //Check For Loading
        if (chunk.loadingState != Chunk.LoadingState.WaitingOnMesh)
            return;
        if (chunk.isCulled)
            return;

        //Update Its Voxels
        chunk.UpdateMesh(results);
    }

    //Edit Voxels
    public void EditVoxelsRequest(Vector3 point, int voxel)
    {
        //Get Coords
        Vector2Int cc = GetChunkCoords(point);
        Vector3Int vc = GetVoxelCoords(point);

        //Check For Transparency
        bool shouldBeTrans = false;
        if (voxel != -1)
        {
            if (VoxelManager.singleton.voxels[voxel].isTransparent)
                shouldBeTrans = true;
        }

        byte voxelIndex = (byte)(voxel + 1);

        //Check Target Chunk
        Chunk targetChunk = chunks[cc];
        if (targetChunk.loadingState != Chunk.LoadingState.Loaded)
            return;

        //Check If Were Only Updating One Chunk
        if (vc.x >= 1 && vc.x < chunkArea - 1 && vc.z >= 1 && vc.z < chunkArea - 1)
        {
            //Update Target Chunk
            targetChunk.SetVoxel(vc, voxelIndex);
            if (shouldBeTrans && !targetChunk.transparent)
                targetChunk.transparent = true;

            RequestMeshUpdate(targetChunk, targetChunk.GetVoxels(), targetChunk.transparent);
            return;
        }

        List<Vector2Int> selectedCoords = new List<Vector2Int>();
        List<Vector3Int> selectedVoxels = new List<Vector3Int>();

        //Check Left
        if(vc.x == 0)
        {
            Vector2Int sc = cc + new Vector2Int(-1, 0);
            if (!chunks.ContainsKey(sc))
                return;

            if (chunks[sc].loadingState != Chunk.LoadingState.Loaded)
                return;

            selectedCoords.Add(sc);
            selectedVoxels.Add(new Vector3Int(chunkArea + 1, vc.y, vc.z + 1));
        }

        //Check Right
        if (vc.x == chunkArea - 1)
        {
            Vector2Int sc = cc + new Vector2Int(1, 0);
            if (!chunks.ContainsKey(sc))
                return;

            if (chunks[sc].loadingState != Chunk.LoadingState.Loaded)
                return;

            selectedCoords.Add(sc);
            selectedVoxels.Add(new Vector3Int(0, vc.y, vc.z + 1));
        }

        //Check Up
        if (vc.z == chunkArea - 1)
        {
            Vector2Int sc = cc + new Vector2Int(0, 1);
            if (!chunks.ContainsKey(sc))
                return;

            if (chunks[sc].loadingState != Chunk.LoadingState.Loaded)
                return;

            selectedCoords.Add(sc);
            selectedVoxels.Add(new Vector3Int(vc.x + 1, vc.y, 0));
        }

        //Check Down
        if (vc.z == 0)
        {
            Vector2Int sc = cc + new Vector2Int(0, -1);
            if (!chunks.ContainsKey(sc))
                return;

            if (chunks[sc].loadingState != Chunk.LoadingState.Loaded)
                return;

            selectedCoords.Add(sc);
            selectedVoxels.Add(new Vector3Int(vc.x + 1, vc.y, chunkArea + 1));
        }

        //Update Target Chunk
        targetChunk.SetVoxel(vc, voxelIndex);
        if (shouldBeTrans && !targetChunk.transparent)
            targetChunk.transparent = true;

        RequestMeshUpdate(targetChunk, targetChunk.GetVoxels(), targetChunk.transparent);

        //Update Target Chunk
        for (int i = 0; i < selectedCoords.Count; i++)
        {
            chunks[selectedCoords[i]].SetVoxel(selectedVoxels[i], voxelIndex, true);
            if (shouldBeTrans && !chunks[selectedCoords[i]].transparent)
                chunks[selectedCoords[i]].transparent = true;

            RequestMeshUpdate(chunks[selectedCoords[i]], chunks[selectedCoords[i]].GetVoxels(), chunks[selectedCoords[i]].transparent);
        }
    }
    #endregion
}

//Chunk Data
public class Chunk
{
    //Variables
    public Vector2Int chunkID;
    public enum LoadingState { Loaded, WaitingOnVoxels, WaitingOnMesh }
    public LoadingState loadingState;
    public bool isCulled;

    private byte[,,] voxelData;
    public Vector3 position;
    public int area;
    public int height;

    public bool transparent;
    Material chunkMaterial;
    Material waterMaterial;

    //Layers
    public GameObject chunkObj;
    public Transform chunkTransform;

    private GameObject solidLayerObj;
    private Transform solidLayerTransform;
    private MeshFilter solidLayerFilter;
    private MeshRenderer solidLayerRenderer;
    private MeshCollider solidLayerColider;

    private GameObject transparentLayerObj;
    private Transform transparentLayerTransform;
    private MeshFilter transparentLayerFilter;
    private MeshRenderer transparentLayerRenderer;

    //Creation
    public Chunk(Vector2Int chunkID, Vector3 position, int area, int height, Material chunkMaterial = null, Material waterMaterial = null)
    {
        //Set Data
        this.chunkID = chunkID;
        this.position = position;
        this.area = area;
        this.height = height;
        voxelData = new byte[area, height, area];

        this.chunkMaterial = chunkMaterial;
        this.waterMaterial = waterMaterial;

        //Make GameObject And Get Its Components
        CreateChunkObject();

        //Start Creation
        WorldGenerationManager.singleton.RequestChunkGeneration(this);
    }

    //Loading And Unloading
    /*
    public void Load()
    {
        UpdateLayers(transparent);
        SetVisability(true);
    }

    public void Unload()
    {
        SetVisability(false);
    }
    */

    public void Load()
    {
        //Update States
        isCulled = false;

        //Create Objects
        CreateChunkObject();
        UpdateLayers(transparent);

        //Get New Mesh
        WorldGenerationManager.singleton.RequestMeshUpdate(this, voxelData, transparent);
    }

    public void Unload()
    {
        //Update States
        isCulled = true;

        //Destroy Objects
        GameObject.Destroy(chunkObj);
    }

    //Updating Functions
    public void SetVisability(bool v)
    {
        isCulled = !v;

        if (chunkObj != null)
        {
            chunkObj.SetActive(v);
        }
    }

    private void CreateChunkObject()
    {
        if(chunkObj != null)
            GameObject.DestroyImmediate(chunkObj);

        //Make GameObject And Get Its Components
        chunkObj = new GameObject($"Chunk: {chunkID}");
        chunkTransform = chunkObj.transform;
        chunkTransform.position = position;
    }

    public void UpdateLayers(bool transparent)
    {
        //Create Solid
        if(solidLayerObj == null)
        {
            solidLayerObj = new GameObject("Solid");
            solidLayerTransform = solidLayerObj.transform;
            solidLayerTransform.position = position;
            solidLayerTransform.SetParent(chunkTransform);

            solidLayerRenderer = solidLayerObj.AddComponent<MeshRenderer>();
            solidLayerRenderer.material = chunkMaterial;
            solidLayerFilter = solidLayerObj.AddComponent<MeshFilter>();
            solidLayerColider = solidLayerObj.AddComponent<MeshCollider>();
        }

        //Check Transparent
        if (transparentLayerObj == null && transparent)
        {
            transparentLayerObj = new GameObject("Transparent");
            transparentLayerTransform = transparentLayerObj.transform;
            transparentLayerTransform.position = position;
            transparentLayerTransform.SetParent(chunkTransform);

            transparentLayerRenderer = transparentLayerObj.AddComponent<MeshRenderer>();
            transparentLayerRenderer.material = waterMaterial;
            transparentLayerFilter = transparentLayerObj.AddComponent<MeshFilter>();
        }

        //Destroy Unused Layers
        if (transparentLayerObj != null && !transparent)
        {
            GameObject.Destroy(transparentLayerObj);
            transparentLayerTransform = null;

            transparentLayerRenderer = null;
            transparentLayerFilter = null;
            transparentLayerObj = null;
        }
    }

    public void UpdateVoxels(byte[,,] voxels, bool t)
    {
        //Set Transparent
        transparent = t;

        //Set Voxels
        this.voxelData = voxels; // new byte[area, height, area];
        /*
        int indexX = 0, indexY = 0, indexZ = 0;
        for (int x = 1; x < area+1; x++)
        {
            for (int z = 1; z < area+1; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    this.voxelData[indexX, indexY, indexZ] = voxels[x, y, z];
                    indexY++;
                }

                indexY = 0;
                indexZ++;
            }

            indexZ = 0;
            indexX++;
        }
        */
    }

    public void UpdateMesh(MeshUpdateResults results)
    {
        loadingState = LoadingState.Loaded;
        
        //Update Layers
        UpdateLayers(transparent);

        //Create Solid Mesh
        Mesh solidMesh = new Mesh();
        solidMesh.vertices = results.solidPass.verticies;
        solidMesh.triangles = results.solidPass.tries;
        solidMesh.uv = results.solidPass.uvs;
        solidMesh.normals = results.solidPass.normals;

        //Set Solid Data
        solidLayerFilter.mesh = solidMesh;
        solidLayerColider.sharedMesh = solidMesh;

        //Set Transparent Data
        if (transparent)
        {
            Mesh transMesh = new Mesh();
            transMesh.vertices = results.transparentPass.verticies;
            transMesh.triangles = results.transparentPass.tries;
            transMesh.uv = results.transparentPass.uvs;
            transMesh.normals = results.transparentPass.normals;

            transparentLayerFilter.mesh = transMesh;
        }
    }

    //Voxel Sampling
    public bool ValidVoxelIndex(Vector3Int coord)
    {
        return (coord.x >= 0 && coord.x < area) && (coord.y >= 0 && coord.y < height) && (coord.z >= 0 && coord.z < area);
    }

    public byte GetVoxel(Vector3Int coord, bool absolute = false)
    {
        if (!ValidVoxelIndex(coord))
        {
            return 0;
        }

        int offset = (absolute)? 0 : 1;
        return voxelData[coord.x + offset, coord.y, coord.z + offset];
    }

    public byte[,,] GetVoxels()
    {
        return voxelData;
    }

    public void SetVoxel(Vector3Int coord, byte voxel, bool absolute = false)
    {
        if (ValidVoxelIndex(coord) && !absolute)
        {
            voxelData[coord.x + 1, coord.y, coord.z + 1] = voxel;
            return;
        }

        voxelData[coord.x, coord.y, coord.z] = voxel;
        return;
    }
}

//Voxel Creation Request
public struct ChunkGenerationRequest
{
    public Vector2Int chunkID;
    public int area;
    public int height;
    public Vector3 position;
}

//Voxel Creation Callbacks
public struct ChunkGenerationResults
{
    public Vector2Int chunkID;
    public byte[,,] voxels;
    public bool useTransparentPass;
}

//Voxel Creation Request
public struct VoxelUpdateRequest
{
    public Vector2Int chunkID;
}

//Mesh Creation Requests
public struct MeshUpdateRequest
{
    public Vector2Int chunkID;
    public Vector3 position;
    public int area;
    public int height;
    public byte[,,] voxelData;
    public bool transparent;
}

//Mesh Creation Callbacks
public struct MeshUpdateResults
{
    public Vector2Int chunkID;
    public ChunkMeshData solidPass;
    public ChunkMeshData transparentPass;
}

public struct ChunkMeshData
{
    public Vector3[] verticies;
    public int[] tries;
    public Vector2[] uvs;
    public Vector3[] normals;
}