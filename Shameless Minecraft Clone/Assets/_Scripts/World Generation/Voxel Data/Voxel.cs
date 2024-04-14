using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Voxel", menuName = "World/Voxel", order = 3)]
public class Voxel : ScriptableObject, System.IComparable
{
    public string Name;
    public byte id;

    [Space]
    public bool isUnbreakable = false;
    public bool isTransparent = false;
    public WorldConstants.BlockForm blockForm = WorldConstants.BlockForm.Default;

    [Header("Texture Information")]
    public string texturePath = "";
    public WorldConstants.TextureWrapMode wrapMode = WorldConstants.TextureWrapMode.Cubemap;

    public int CompareTo(object obj)
    {
        try
        {
            Voxel other = (Voxel)obj;

            if(id > other.id)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }
        catch (System.Exception ex)
        {
            throw ex;
        }
    }
}
