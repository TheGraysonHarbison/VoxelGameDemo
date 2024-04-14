using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldConstants
{
    public enum TextureWrapMode
    {
        Single = 0,
        Triple = 1,
        Cubemap = 2
    };

    public enum BlockForm
    {
        Default = 0,
        Water = 1,
    };

    public enum FaceDirections
    {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3,
        Front = 4,
        Back = 5
    }
}
