using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class TreeHashGrid
{
    System.Random random;
    private int size;
    private TreeGenerationSettings settings;

    Vector2 offset;
    private bool[,] placementGrid;
    private int[,] seedGrid;

    //Constructor
    public TreeHashGrid(int seed, int size, TreeGenerationSettings settings)
    {
        random = new System.Random(seed);
        this.size = size;
        this.settings = settings;

        placementGrid = new bool[size, size];
        seedGrid = new int[size, size];

        Generate();
    }

    //Generation
    private void Generate()
    {
        //Get Offset
        offset.x = random.Next(-100000, 100000);
        offset.y = random.Next(-100000, 100000);

        int step = size / settings.gridDivisions;

        //Loop To Correct Positions
        for (int x = 0; x < size; x+=step)
        {
            for (int z = 0; z < size; z += step)
            {
                //Get Points
                int indexX = x + random.Next(0, step);
                int indexZ = z + random.Next(0, step);

                indexX = Mathf.Clamp(indexX, 0, size-1);
                indexZ = Mathf.Clamp(indexZ, 0, size-1);

                //Set Randoms
                placementGrid[indexX, indexZ] = true;
                seedGrid[indexX, indexZ] = random.Next(0, 100);
            }
        }
    }

    //Sampling
    private Vector2Int GetPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x + offset.x) % size;
        int z = Mathf.FloorToInt(position.z + offset.y) % size;

        //Check For Negative
        if (x < 0)
            x = size - Mathf.Abs(x);

        if (z < 0)
            z = size - Mathf.Abs(z);

        return new Vector2Int(x, z);
    }

    //Grids
    public bool PlaceTree(Vector3 coords)
    {
        Vector2Int c = GetPosition(coords);
        return placementGrid[c.x,c.y];
    }

    public int SeedInfo(Vector3 coords)
    {
        Vector2Int c = GetPosition(coords);
        return seedGrid[c.x, c.y];
    }
}

[System.Serializable]
public class TreeGenerationSettings
{
    public int gridDivisions = 4;
}