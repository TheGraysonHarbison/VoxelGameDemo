using System.Collections.Generic;
using UnityEngine;


public static class VoxelMapper
{
    const float SURFACE_THICKNESS = 0.5f;
    const float SOIL_THICKNESS = 5;

    //Produce Noise Map
    public static float[,] MakeNoiseMap(int size, Vector2 offset, NoiseSettings noiseSettings)
    {
        return new NoiseSampler(0, noiseSettings).SampleMap2D(size, offset);
    }

    public static float[,,] MakeNoiseMap(int size, int height, Vector3 offset, NoiseSettings noiseSettings)
    {
        return new NoiseSampler(0, noiseSettings).SampleMap3D(size, height, offset);
    }

    //The Voxel Calculation Tasks 
    public static ChunkGenerationResults CalculateNewVoxels(ChunkGenerationRequest request)
    {
        byte[,,] voxels = new byte[request.area + 2, request.height, request.area + 2];

        //Run Passes
        CanvasPass(voxels, request.area, request.height, request.position, out float[,] heightMap, out bool useTransparentPass);
        BiomePass(voxels, heightMap, request.area, request.height, request.position, out int[,] biomeMap);
        CavePass(voxels, heightMap, request.area, request.height, request.position);
        DecorationPass(voxels, heightMap, biomeMap, request.area, request.height, request.position);

        //Return
        return new ChunkGenerationResults()
        {
            chunkID = request.chunkID,
            voxels = voxels,
            useTransparentPass = useTransparentPass
        };
    }

    //Canvas Pass
    // Makes The Basic Shape Of The World And Fill Water
    private static void CanvasPass(in byte[,,] voxels, int area, int height, Vector3 worldPosition, out float[,] heightMap, out bool useTransparentPass)
    {
        //Get Coords
        Vector2 flatCoords = new Vector2(worldPosition.x, worldPosition.z);

        //Fill
        int indexX = 0, indexZ = 0;

        heightMap = new float[area + 2, area + 2];
        useTransparentPass = false;

        for (int x = -1; x < area + 1; x++)
        {
            for (int z = -1; z < area + 1; z++)
            {
                //Find Surface Level
                float surfaceLevel = WorldGenerationManager.singleton.GetWorldHeight(flatCoords + new Vector2(x, z));
                heightMap[indexX, indexZ] = surfaceLevel;

                //Check Y
                for (int y = 0; y < height; y++)
                {
                    /*
                    //Determine Ground Level
                    if (y < surfaceLevel + SURFACE_THICKNESS)
                    {
                        voxels[indexX, y, indexZ] = 3;
                    }
                    else
                    {
                        //Determin Air Or Water
                        if(y < WorldGenerationManager.singleton.BaseSurfaceLevel + SURFACE_THICKNESS - 3)
                        {
                            voxels[indexX, y, indexZ] = 5;
                            useTransparentPass = true;
                            continue;
                        }

                        voxels[indexX, y, indexZ] = 0;
                    }
                    */

                    bool wantToTrans = false;
                    int solve = SolveCanvasLayers(y, surfaceLevel, out wantToTrans);

                    voxels[indexX, y, indexZ] = (byte)solve;
                    if(wantToTrans)
                        useTransparentPass = true;
                }

                indexZ++;
            }

            indexZ = 0;
            indexX++;
        }
    }

    private static int SolveCanvasLayers(int yLevel, float surfaceLevel, out bool useTransparentPass)
    {
        //Determine Ground Level
        useTransparentPass = false;

        if (yLevel == 0)
            return 8;

        if (yLevel < surfaceLevel + SURFACE_THICKNESS)
        {
            return 3;
        }
        else
        {
            //Determin Air Or Water
            if (yLevel < WorldGenerationManager.singleton.BaseSurfaceLevel + SURFACE_THICKNESS - 3)
            {
                useTransparentPass = true;
                return 5;
            }

            return 0;
        }
    }

    //Biome Pass
    //Determains The Surface Typing
    private static void BiomePass(in byte[,,] voxels, in float[,] heightMap, int area, int height, Vector3 worldPosition, out int[,] biomeMap)
    {
        //Get Coords
        Vector2 flatCoords = new Vector2(worldPosition.x, worldPosition.z);
        biomeMap = new int[area+2, area+2];

        //Fill
        int indexX = 0, indexZ = 0;
        for (int x = -1; x < area + 1; x++)
        {
            for (int z = -1; z < area + 1; z++)
            {
                float surfaceLevel = heightMap[indexX, indexZ];

                //Get Biome
                int biomeIndex = GetChunkBiome(flatCoords + new Vector2(x, z));
                biomeMap[indexX, indexZ] = biomeIndex;
                BiomeSettings biome = WorldGenerationManager.singleton.worldGenerationSettings.Biomes[biomeIndex];

                //Check Y
                for (int y = 0; y < height; y++)
                {
                    /*
                    //Check If Above
                    if (y > surfaceLevel + SURFACE_THICKNESS)
                    {
                        continue;
                    }

                    //Check For Beach
                    if(voxels[indexX, y, indexZ] == 5)
                    {
                        continue;
                    }

                    if(NearWater(indexX, y, indexZ, 3, area, height, voxels))
                    {
                        voxels[indexX, y, indexZ] = 4;
                        continue;
                    }


                    //Check If On Ground
                    if (y >= surfaceLevel - SURFACE_THICKNESS && y <= surfaceLevel + SURFACE_THICKNESS)
                    {
                        voxels[indexX, y, indexZ] = (byte)(biome.surfaceLayerID + 1);
                        continue;
                    }

                    //Check If Below Ground
                    if (y < surfaceLevel - SURFACE_THICKNESS && y >= (surfaceLevel - SOIL_THICKNESS) + SURFACE_THICKNESS)
                    {
                        voxels[indexX, y, indexZ] = (byte)(biome.soilLayerID + 1);
                        continue;
                    }
                    */

                    //Skip Air
                    if (voxels[indexX, y, indexZ] == 0)
                    {
                        continue;
                    }

                    //Check For Beach
                    if (voxels[indexX, y, indexZ] == 5)
                    {
                        continue;
                    }

                    //Check For Sea
                    if (NearWater(indexX, y, indexZ, worldPosition, 2, area, height, voxels))
                    {
                        voxels[indexX, y, indexZ] = 4;
                        continue;
                    }

                    //Solve Biome
                    int solve = SolveBiomeLayers(y, surfaceLevel, biome);
                    if(solve != -1)
                        voxels[indexX, y, indexZ] = (byte)solve;
                }

                indexZ++;
            }


            indexZ = 0;
            indexX++;
        }
    }

    private static int SolveBiomeLayers(int yLevel, float surfaceLevel, BiomeSettings biome)
    {
        //Check If On Ground
        if (yLevel >= surfaceLevel - SURFACE_THICKNESS && yLevel <= surfaceLevel + SURFACE_THICKNESS)
        {
            return biome.surfaceLayerID + 1;
        }

        //Check If Below Ground
        if (yLevel < surfaceLevel - SURFACE_THICKNESS && yLevel >= (surfaceLevel - SOIL_THICKNESS) + SURFACE_THICKNESS)
        {
            return biome.soilLayerID + 1;
        }

        //Check If Above
        return -1;
    }

    //Cave Pass
    private static void CavePass(in byte[,,] voxels, in float[,] heightMap, int area, int height, Vector3 worldPosition)
    {
        //Get Coords
        Vector2 flatCoords = new Vector2(worldPosition.x, worldPosition.z);

        int indexX = 0, indexZ = 0;
        for (int x = -1; x < area + 1; x++)
        {
            for (int z = -1; z < area + 1; z++)
            {
                float surfaceLevel = heightMap[indexX, indexZ];

                //Check Y
                for (int y = 0; y < height; y++)
                {
                    //Check If Above
                    if (y > surfaceLevel + SURFACE_THICKNESS || y == 0)
                    {
                        continue;
                    }


                    if (voxels[indexX, y, indexZ] == 5)
                    {
                        continue;
                    }

                    Vector3 position = new Vector3(x, y, z) + worldPosition;
                    voxels[indexX, y, indexZ] = (byte)SolveCaveLayers(position, heightMap[indexX, indexZ], voxels[indexX, y, indexZ]);
                    /*
                    //Sample Noise Maps
                    float cave = WorldGenerationManager.singleton.GetCaveNoise(position);
                    float mask = WorldGenerationManager.singleton.GetMaskNoise(position);

                    //Sample Gradient
                    float gradient = SampleGradient(position, heightMap[indexX, indexZ]);

                    //Check Threshold
                    float result = (cave * mask) * gradient;
                    if(result >= WorldGenerationManager.singleton.worldGenerationSettings.surfaceThreshold)
                    {
                        voxels[indexX, y, indexZ] = 0;
                    }
                    */
                }

                indexZ++;
            }


            indexZ = 0;
            indexX++;
        }
    }

    private static int SolveCaveLayers(Vector3 position, float height, int initailVoxel)
    {
        //Sample Noise Maps
        float cave = WorldGenerationManager.singleton.GetCaveNoise(position);
        float mask = WorldGenerationManager.singleton.GetMaskNoise(position);

        //Sample Gradient
        float gradient = SampleGradient(position, height);

        //Check Threshold
        float result = (cave * mask) * gradient;
        if (result >= WorldGenerationManager.singleton.worldGenerationSettings.surfaceThreshold)
        {
            return 0;
        }

        return initailVoxel;
    }

    //Decoration Pass
    private static void DecorationPass(in byte[,,] voxels, in float[,] heightMap, int[,] biomeMap, int area, int height, Vector3 worldPosition)
    {
        //Get Coords
        Vector2 flatCoords = new Vector2(worldPosition.x, worldPosition.z);
        TreeHashGrid treeHashGrid = WorldGenerationManager.singleton.GetTreeGrid();

        //Generate All Trees
        List<StructureData> trees = GenerateTrees(area, height, worldPosition, treeHashGrid);

        //Intersect With Chunk
        if (trees.Count != 0)
        {
            ChunkIntersection(trees, area, height, voxels, worldPosition);
        }

        /*
        //Fill
        int indexX = 0, indexZ = 0;
        for (int x = -1; x < area + 1; x++)
        {
            for (int z = -1; z < area + 1; z++)
            {
                float surfaceLevel = heightMap[indexX, indexZ];

                //Get Biome
                int biomeIndex = biomeMap[indexX, indexZ];
                BiomeSettings biome = WorldGenerationManager.singleton.worldGenerationSettings.Biomes[biomeIndex];

                //Check For Any Trees
                if (biome.trees == null)
                    continue;

                if (biome.trees.Length == 0)
                    continue;

                //Check For Soil
                int y = Mathf.FloorToInt(surfaceLevel + SURFACE_THICKNESS);
                Vector3 pos = new Vector3(x, y, z) + worldPosition;

                if (voxels[indexX, y, indexZ] == 1 || voxels[indexX, y, indexZ] == 2)
                {
                    //Start Tree Operation
                    if (treeHashGrid.PlaceTree(pos))
                    {
                        voxels[indexX, y + 1, indexZ] = 6;
                    }
                }

                indexZ++;
            }


            indexZ = 0;
            indexX++;
        }
        */
    }


    //Get A Chunks Biome
    private static int GetChunkBiome(Vector2 coords)
    {
        int bestIndex = 0;
        float tempurature = WorldGenerationManager.singleton.GetTempurature(coords);
        float moisture = WorldGenerationManager.singleton.GetMoisture(coords);

        //Loop
        BiomeSettings[] biomes = WorldGenerationManager.singleton.worldGenerationSettings.Biomes;
        float BestDistance = float.MaxValue;

        for (int i = 0; i < biomes.Length; i++)
        {
            float centerX = biomes[i].MinTemperature + ((biomes[i].MaxTemperature - biomes[i].MinTemperature) / 2f);
            float centerY = biomes[i].MinMoisture + ((biomes[i].MaxMoisture - biomes[i].MinMoisture) / 2f);
            Vector2 target = new Vector2(centerX, centerY);

            float dst = Vector2.Distance(new Vector2(tempurature, moisture), target);
            if (dst <= BestDistance)
            {
                BestDistance = dst;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    //Water Check
    private static bool NearWater(int x, int y, int z, Vector3 worldPosition, int radius, int area, int height,  byte[,,] voxels)
    {
        for (int xO = -radius; xO < radius; xO++)
        {
            for (int zO = -radius; zO < radius; zO++)
            {
                int targetX = x + xO + Mathf.FloorToInt(worldPosition.x);
                int targetZ = z + zO + Mathf.FloorToInt(worldPosition.z);
                float surfaceLevel = WorldGenerationManager.singleton.GetWorldHeight(new Vector2(targetX, targetZ));


                for (int yO = -radius; yO < radius; yO++)
                {
                    int targetY = y + yO;

                    //Solve For Water
                    bool wantToTrans = false;
                    int solve = SolveCanvasLayers(targetY, surfaceLevel, out wantToTrans);
                    if (solve == 5)
                        return true;

                    /*
                    //Check Bounds
                    bool inX = x + xO >= 0 && x + xO <= area;
                    bool inY = y + yO >= 0 && y + yO < height;
                    bool inZ = z + zO >= 0 && z + zO <= area;

                    if (inX && inY && inZ)
                    {
                        //Check Water And Air
                        if (voxels[x+xO, y+yO, z+zO] == 5 && voxels[x, y, z] != 0)
                        {
                            return true;
                        }
                    }
                    */
                }
            }
        }


        return false;
    }

    //Height Gradient
    private static float SampleGradient(Vector3 point, float height)
    {
        float t = Mathf.InverseLerp(0, Mathf.Max(3, height), point.y);
        return WorldGenerationManager.singleton.EvaluateWorldSpline(t,
            WorldGenerationManager.singleton.worldGenerationSettings.CaveGradientSpline);
    }

    //Create Trees In Area
    private static List<StructureData> GenerateTrees(int area, int height, Vector3 worldPosition, TreeHashGrid treeHashGrid)
    {
        List<StructureData > trees = new List<StructureData>();

        //Sample Grid
        int min = -1;
        int max = area + 1;

        min -= area;
        max += area;

        for (int x = min; x < max; x++)
        {
            for (int z = min; z < max; z++)
            {
                //Check For Tree
                Vector3 treeSamplePosition = new Vector3(x, 0, z) + worldPosition;
                Vector2 flat = new Vector2(treeSamplePosition.x, treeSamplePosition.z);

                if (!treeHashGrid.PlaceTree(treeSamplePosition))
                    continue;

                //Check Soil Voxel
                float surfaceLevel = WorldGenerationManager.singleton.GetWorldHeight(flat);
                int y = Mathf.FloorToInt(surfaceLevel + SURFACE_THICKNESS);

                int solve = SolveCanvasLayers(y, surfaceLevel, out bool wantToTrans);
                if (solve == 5)
                    continue;

                int biomeIndex = GetChunkBiome(flat);
                BiomeSettings biome = WorldGenerationManager.singleton.worldGenerationSettings.Biomes[biomeIndex];

                if (biome.trees == null)
                    continue;

                if (biome.trees.Length <= 0)
                    continue;

                solve = SolveBiomeLayers(y, surfaceLevel, biome);
                if(solve != 1 && solve != 2)
                    continue;

                if (solve == 4)
                    continue;

                solve = SolveCaveLayers(treeSamplePosition + new Vector3(0, y, 0), surfaceLevel, solve);
                if (solve == 0)
                    continue;

                //Check Above Soil
                solve = SolveCanvasLayers(y + 1, surfaceLevel, out wantToTrans);
                if (solve != 0)
                    continue;

                //Start Tree
                Vector3Int startingPosition = new Vector3Int(
                    Mathf.FloorToInt(treeSamplePosition.x),
                    y + 1,
                    Mathf.FloorToInt(treeSamplePosition.z)
                    );

                int seed = treeHashGrid.SeedInfo(treeSamplePosition);
                trees.Add(TreeStepper(startingPosition, seed, biome));
            }
        }

        return trees;
    }

    private static StructureData TreeStepper(Vector3Int position, int seed, BiomeSettings biome)
    {
        //Get Tree
        System.Random random = new System.Random(seed);
        VoxelTree treeData = biome.trees[random.Next(biome.trees.Length)];

        //Calculate The Trees Dimensions
        int radius = random.Next(treeData.leaveRadiusMinimum, treeData.leaveRadiusMaximum);
        int width = 1 + (radius * 2);

        int trunkLength = random.Next(treeData.minTrunkHeight, treeData.maxTrunkHeight);
        int height = trunkLength + treeData.leaveScale - treeData.leavesDistanceFromTopOfTrunk;

        int leaveStartHeight = trunkLength - treeData.leavesDistanceFromTopOfTrunk;

        byte[,,] voxels = new byte[width, height, width];

        //Add Leaves

        Vector3 core = new Vector3(radius, leaveStartHeight, radius);
        for (int y = leaveStartHeight; y < height; y++)
        {
            //Shrink Toward Top
            float r = radius;
            if(y > leaveStartHeight + treeData.leavesDistanceFromTopOfTrunk) //Start Shrink
            {
                float t = Mathf.InverseLerp(leaveStartHeight, height, y);
                r = Mathf.Lerp(radius, treeData.leaveRadiusMinimum, t);
            }

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < width; z++)
                {
                    //Get Distance To Core
                    float dist = Vector3.Distance(core, new Vector3(x, y, z));

                    if (dist <= r)
                        voxels[x, y, z] = (byte)treeData.leavesID;
                }
            }
        }

        //Add Trunk
        for (int y = 0; y < trunkLength; y++)
        {
            voxels[radius, y, radius] = (byte)treeData.trunkID;
        }

        //Set Data And Return
        StructureData tree = new StructureData();
        tree.worldStartPosition = position - new Vector3Int(radius, 0, radius);
        tree.worldEndPosition = position + new Vector3Int(radius, height, radius);

        tree.structureVoxels = voxels;
        tree.sizeX = width;
        tree.sizeY = height;
        tree.sizeZ = width;

        return tree;
    }

    //Chunk Structure Intersection
    private static void ChunkIntersection(List<StructureData> structures, int area, int height, in byte[,,] voxels, Vector3 worldPosition)
    {
        try
        {
            for (int i = 0; i < structures.Count; i++)
            {

                //Check For Intersecting
                //bool intersect = false;
                Vector3 chunkMin = worldPosition + new Vector3(-2, 0, -2);
                Vector3 chunkMax = worldPosition + new Vector3(area + 2, height, area + 2);

                /*
                if (structures[i].worldStartPosition.x >= chunkMin.x && structures[i].worldStartPosition.x <= chunkMax.x)
                {
                    if (structures[i].worldEndPosition.x >= chunkMin.x && structures[i].worldEndPosition.x <= chunkMax.x)
                    {
                        intersect = true;
                    }
                }


                if (structures[i].worldStartPosition.z >= chunkMin.z && structures[i].worldStartPosition.z <= chunkMax.z)
                {
                    if (structures[i].worldEndPosition.z >= chunkMin.z && structures[i].worldEndPosition.z <= chunkMax.z)
                    {
                        intersect = true;
                    }
                }

                if (!intersect)
                    continue;
                */

                //Add Intersecting Voxels
                for (int x = 0; x < structures[i].sizeX; x++)
                {
                    for (int y = 0; y < structures[i].sizeY; y++)
                    {
                        for (int z = 0; z < structures[i].sizeZ; z++)
                        {
                            Vector3Int sVoxelCoords = new Vector3Int();
                            sVoxelCoords.x = structures[i].worldStartPosition.x + x - Mathf.FloorToInt(worldPosition.x);
                            sVoxelCoords.y = structures[i].worldStartPosition.y + y;
                            sVoxelCoords.z = structures[i].worldStartPosition.z + z - Mathf.FloorToInt(worldPosition.z);

                            //Bounds
                            if (sVoxelCoords.x < -1 || sVoxelCoords.x >= area + 1)
                                continue;

                            if (sVoxelCoords.y < 0 || sVoxelCoords.y >= height)
                                continue;

                            if (sVoxelCoords.z < -1 || sVoxelCoords.z >= area + 1)
                                continue;

                            //Logic
                            sVoxelCoords += new Vector3Int(1, 0, 1);

                            if (!structures[i].replace && voxels[sVoxelCoords.x, sVoxelCoords.y, sVoxelCoords.z] != 0)
                                continue;

                            voxels[sVoxelCoords.x, sVoxelCoords.y, sVoxelCoords.z] = structures[i].structureVoxels[x, y, z];
                        }
                    }
                }
            }
        }
        catch(System.Exception e)
        {
            Debug.LogException(e);
        }
    }

    //Structure Class
    public class StructureData
    {
        public byte[,,] structureVoxels;
        public int sizeX;
        public int sizeY;
        public int sizeZ;
        public Vector3Int worldStartPosition;
        public Vector3Int worldEndPosition;
        public bool replace = false;
    }
}
