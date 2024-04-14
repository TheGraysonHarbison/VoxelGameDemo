using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Biome Settings", menuName = "World/Biome Settings", order = 1)]
public class BiomeSettings : ScriptableObject
{
    public new string name = "";
    public Color Color;

    [Header("Requirements")]
    [Range(-1, 1)] public float MaxTemperature;
    [Range(-1, 1)] public float MinTemperature;
    [Space]
    [Range(-1, 1)] public float MaxMoisture;
    [Range(-1, 1)] public float MinMoisture;

    [Header("Ground Compostition")]
    public int surfaceLayerID = 1;
    public int soilLayerID = 0;
    public int stoneLayerID = 2;
    [Space]
    public int StoneOffset = 4;

    [Header("Trees")]
    public VoxelTree[] trees;
}
