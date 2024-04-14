using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tree", menuName = "World/Tree", order = 2)]
public class VoxelTree : ScriptableObject
{
    [Header("Voxels")]
    public int trunkID = 1;
    public int leavesID = 1;

    [Header("Generation Settings")]
    public int minTrunkHeight = 5;
    public int maxTrunkHeight = 9;

    [Space]
    public int leavesDistanceFromTopOfTrunk = 2;
    public int leaveScale = 2;

    [Space]
    public int leaveRadiusMinimum = 3;
    public int leaveRadiusMaximum = 5;
}
