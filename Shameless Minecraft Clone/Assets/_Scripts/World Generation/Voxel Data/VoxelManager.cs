using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class VoxelManager : MonoBehaviour
{
    //Singleton
    public static VoxelManager singleton;
    public VoxelManager()
    {
        if (singleton == null)
            singleton = this;
    }

    //Settings
    public int pixelsPerImage = 16;
    public int tilesPerSide = 64;
    [Space]
    public Voxel[] voxels;
    public Texture2D TextureAtlas { get { return atlas; } private set { } }

    //Variables
    private Texture2D atlas;
    private int[] textureSkips;

    string savePath = "Assets/Resources/Atlas/";
    string atlasFileName = "AtlasTex.png";
    string infoFileName = "AtlasInfo.json";

    private int[,] samplingFaceOffsets =
    {
        {0, 0, 0, 0, 0, 0}, //Single
        {2, 0, 1, 1, 1, 1}, //Triple
        {0, 0, 0, 0, 0, 0} //Cubemap
    };

    //Find Texture Coords
    public Vector2 SampleAtlas(int index, WorldConstants.FaceDirections direction)
    {
        //Get Voxel
        Voxel voxel = voxels[index];

        //Calculate Starting Coords
        int max = pixelsPerImage * tilesPerSide;

        int textureOffset = textureSkips[voxel.id];
        int extraOffset = samplingFaceOffsets[(int)voxel.wrapMode, (int)direction];
        Vector2 coord = TextureCoordsFromIndex(textureOffset + extraOffset);

        //Apply Sampling
        float x = Mathf.InverseLerp(0, max, coord.x);
        float y = Mathf.InverseLerp(0, max, coord.y);
        Vector2 uvA = new Vector2(x, y); // 0 0
        return uvA;

        /*
        x = Mathf.InverseLerp(0, max, coord.x + pixelsPerImage);
        y = Mathf.InverseLerp(0, max, coord.y);
        Vector2 uvB = new Vector2(x, y); // 1 0

        x = Mathf.InverseLerp(0, max, coord.x + pixelsPerImage);
        y = Mathf.InverseLerp(0, max, coord.y + pixelsPerImage);
        Vector2 uvC = new Vector2(x, y); // 1 1

        x = Mathf.InverseLerp(0, max, coord.x);
        y = Mathf.InverseLerp(0, max, coord.y + pixelsPerImage);
        Vector2 uvD = new Vector2(x, y); // 0 1

        //Return
        return new Vector2[] { uvA, uvB, uvC, uvD };
        */
    }

    #region Loading And Generating
    //We Do Not Load Or Save The Atlas During Runtime Anymore

    //Load Atlas
    public void LoadAtlas()
    {
        Debug.Log("Loading Atlas Information");
        if (!HasValidAtlas())
        {
            Debug.Log("Atlas Nonexistent");
            return;
        }

        //Load Information
        string json = File.ReadAllText(savePath + infoFileName);
        AtlasInformation information = JsonUtility.FromJson<AtlasInformation>(json);

        //Key Check
        if(information.VoxelKey != MakeVoxelKey())
        {
            Debug.Log("Atlas Invalid, Regenerating");
            GenerateAtlas();
            return;
        }

        //Get And Set
        atlas = Resources.Load<Texture2D>("Atlas/AtlasTex");
        atlas.filterMode = FilterMode.Point;
        atlas.Apply();

        textureSkips = information.textureOffsets;
        Debug.Log("Atlas Information Loaded");
    }

    //Make Atlas
    public void GenerateAtlas(bool saveToFile = false)
    {
        Debug.Log("Generating New Atlas Information");
        SortVoxels();

        //Init Atlas
        int width = pixelsPerImage * tilesPerSide;
        atlas = new Texture2D(width, width, TextureFormat.RGBA32, false);
        atlas.filterMode = FilterMode.Point;

        //Map Every Voxel
        int currentIndex = 0;

        List<int> offsets = new List<int>();
        foreach (Voxel voxel in voxels)
        {
            //Load In Voxel Info
            WorldConstants.TextureWrapMode wrapMode = voxel.wrapMode;
            int index = voxel.id;
            Texture2D voxelTexture = Resources.Load<Texture2D>(voxel.texturePath);

            //Verify
            if (voxelTexture == null)
                throw new System.Exception($"Path {voxel.texturePath} Is Invalid");

            //Add To Atlas Depanding On Wrap Mode
            switch (wrapMode)
            {
                case WorldConstants.TextureWrapMode.Single:
                    AddToAtlas(currentIndex, new Texture2D[] { voxelTexture });

                    offsets.Add(currentIndex);
                    currentIndex++;
                    break;

                case WorldConstants.TextureWrapMode.Triple:
                    Texture2D[] slices = SpliceTriple(voxelTexture);

                    AddToAtlas(currentIndex, slices);
                    offsets.Add(currentIndex);
                    currentIndex += 3;
                    break;

                case WorldConstants.TextureWrapMode.Cubemap:


                    break;
            }
        }

        //Save Data
        textureSkips = offsets.ToArray();
        atlas.filterMode = FilterMode.Point;
        
        atlas.Apply();
#if UNITY_EDITOR
        if (!saveToFile)
            return;

        byte[] atlasData = atlas.EncodeToPNG();
        File.WriteAllBytes(savePath + atlasFileName, atlasData);

        //Save JSON Information
        AtlasInformation info = new AtlasInformation()
        {
            VoxelKey = MakeVoxelKey(),
            textureOffsets = textureSkips
        };

        string json = JsonUtility.ToJson(info);
        File.WriteAllText(savePath + infoFileName, json);

        //Editor Refresh
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    //Verify Files
    public bool HasValidAtlas()
    {
        return File.Exists(savePath + atlasFileName) && File.Exists(savePath + infoFileName);
    }

    //Texture Manipulation
    private void AddToAtlas(int index, Texture2D[] textures)
    {
        for (int i = 0; i < textures.Length; i++)
        {
            Vector2Int atlasCoords = TextureCoordsFromIndex(index);
            Color[] pixels = textures[i].GetPixels(0, 0, pixelsPerImage, pixelsPerImage);

            atlas.SetPixels(atlasCoords.x, atlasCoords.y, pixelsPerImage, pixelsPerImage, pixels);
            index++;
        }
    }

    private Texture2D[] SpliceTriple(Texture2D tex)
    {
        Texture2D bottom = new Texture2D(pixelsPerImage, pixelsPerImage);
        bottom.filterMode = FilterMode.Point;
        Color[] pixels = tex.GetPixels(0, 0, pixelsPerImage, pixelsPerImage);
        bottom.SetPixels(pixels);
        bottom.Apply();

        Texture2D side = new Texture2D(pixelsPerImage, pixelsPerImage);
        side.filterMode = FilterMode.Point;
        pixels = tex.GetPixels(pixelsPerImage, 0, pixelsPerImage, pixelsPerImage);
        side.SetPixels(pixels);
        side.Apply();

        Texture2D top = new Texture2D(pixelsPerImage, pixelsPerImage);
        top.filterMode = FilterMode.Point;
        pixels = tex.GetPixels(pixelsPerImage * 2, 0, pixelsPerImage, pixelsPerImage);
        top.SetPixels(pixels);
        top.Apply();

        return new Texture2D[] { bottom, side, top };
    }

    private void SpliceCubemap(Texture2D tex)
    {

    }
    #endregion

    private Vector2Int TextureCoordsFromIndex(int index)
    {
        //Overflow Check
        if (index >= tilesPerSide * tilesPerSide)
            throw new System.Exception("Index Outside Atlas");

        //Find Position
        int x = 0;
        int y = 0;

        if(index < tilesPerSide)
        {
            x = index;
        }
        else
        {
            x = index % tilesPerSide;
            y = index / tilesPerSide;
        }

        return new Vector2Int(x, y) * pixelsPerImage;
    }

    //Key Gen
    private int MakeVoxelKey()
    {
        int key = voxels.Length;

        for (int i = 0; i < voxels.Length; i++)
        {
            key += i;
        }

        return key;
    }

    //Voxel Sampling
    public Voxel GetVoxelFromIndex(byte i)
    {
        //Check Range
        int index = (int)i-1;
        if (index < 0 || index >= voxels.Length)
        {
            Debug.Log($"Voxel \"{index}\" Does Not Exist");
            return null;
        };

        //Return
        return voxels[index];
    }

    public Voxel GetVoxelFromIndex(int i)
    {
        //Check Range
        int index = i;
        if (index < 0 || index >= voxels.Length)
        {
            Debug.Log($"Voxel \"{index}\" Does Not Exist");
            return null;
        };

        //Return
        return voxels[index];
    }

    public void SortVoxels()
    {
        System.Array.Sort(voxels);
    }
}

//Atlas Json Info
[System.Serializable]
public struct AtlasInformation
{
    public int VoxelKey;
    public int[] textureOffsets;
}