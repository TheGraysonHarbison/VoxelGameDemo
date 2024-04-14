using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoiseSettings
{
    public FastNoiseLite.NoiseType NoiseType = FastNoiseLite.NoiseType.Perlin;
    public float Scale = 0.2f;
    public Vector3 Offset;
    [Space]
    public float Power = 1;

    [Header("Fractal Settings")]
    public FastNoiseLite.FractalType FractalType = FastNoiseLite.FractalType.None;
    public int Octaves = 4;
    public float Lacunarity = 1f;
    public float Persitance = 0.5f;

    public void UpdateNoise(in FastNoiseLite noise)
    {
        noise.SetNoiseType(NoiseType);
        noise.SetFractalType(FractalType);
        noise.SetFractalOctaves(Octaves);

        noise.SetFractalLacunarity(Lacunarity);
        noise.SetFractalWeightedStrength(Persitance);
    }
}