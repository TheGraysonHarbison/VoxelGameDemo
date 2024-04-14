using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New World Generation Settings", menuName = "World/World Generation Settings", order = 0)]
public class WorldGenerationSettings : ScriptableObject
{
    public new string name;

    [Header("Generation Settings")]
    public NoiseSettings ElevationNoiseSettings;
    public WorldSplinePoint[] ElevationSpline;
    [Space]
    public NoiseSettings ErosionNoiseSettings;
    public WorldSplinePoint[] ErosionSpline;
    [Space]
    public NoiseSettings PeaksNoiseSettings;
    public WorldSplinePoint[] PeaksSpline;

    [Header("Biome Settings")]
    public NoiseSettings TemperatureNoiseSettings;
    public WorldSplinePoint[] TempuratureSpline;
    [Space]
    public NoiseSettings MoistureNoiseSettings;
    public WorldSplinePoint[] MoistureSpline;

    [Space]
    public BiomeSettings[] Biomes;

    [Header("Tree Generation")]
    public TreeGenerationSettings TreeGenerationSettings;

    [Header("Cave Settings")]
    public float surfaceThreshold = 0.5f;
    [Space]
    public NoiseSettings CaveNoiseSettings;
    public NoiseSettings CaveMaskNoiseSettings;
    public WorldSplinePoint[] CaveGradientSpline;

    public enum DisplayDataType
    {
        None,
        Masks,
        Biomes,
        Elevation,
        Erosion,
        Peaks,
        World,
        Trees
    };
    
    [Header("Debugging")]
    public DisplayDataType displayType = DisplayDataType.Masks;
}

[System.Serializable]
public struct WorldSplinePoint
{
    public float time;
    public float value;
}