using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseSampler
{
    NoiseSettings noiseSettings;
    FastNoiseLite noise;

    //Constructor
    public NoiseSampler(int seed, NoiseSettings noiseSettings)
    {
        this.noiseSettings = noiseSettings;

        noise = new FastNoiseLite(seed);
        noiseSettings.UpdateNoise(noise);
    }

    //Sample A Single Point In 2D
    public float Sample2D(Vector3 point)
    {
        float sampleX = (point.x - noiseSettings.Offset.y) / noiseSettings.Scale;
        float sampleY = (point.y - noiseSettings.Offset.x) / noiseSettings.Scale;
        return noise.GetNoise(sampleX, sampleY) * noiseSettings.Power;
    }

    //Sample And Fill A Map In 2D
    public float[,] SampleMap2D(int size, Vector3 point)
    {
        float[,] map = new float[size, size];

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                map[x, y] = Sample2D(new Vector3(x, y) + point);
            }
        }
    
        return map;
    }

    //Sample A Single Point In 3D
    public float Sample3D(Vector3 point)
    {
        float sampleX = (point.x - noiseSettings.Offset.x) / noiseSettings.Scale;
        float sampleY = (point.y + noiseSettings.Offset.y) / noiseSettings.Scale;
        float sampleZ = (point.z - noiseSettings.Offset.z) / noiseSettings.Scale;

        return noise.GetNoise(sampleX, sampleY, sampleZ) * noiseSettings.Power;
    }

    //Sample And Fill A Map In 3D
    public float[,,] SampleMap3D(int size, int height, Vector3 point)
    {
        float[,,] map = new float[size, height, size];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    map[x, y, z] = Sample3D(point + new Vector3(x, y, z));
                }
            }
        }

        return map;
    }
}
