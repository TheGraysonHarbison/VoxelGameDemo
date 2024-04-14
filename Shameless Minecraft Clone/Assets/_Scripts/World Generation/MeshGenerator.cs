using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeshGenerator
{
    static bool UseGreedyMeshing = true;

    private static Vector3Int[] CoordOffsets =
    {
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.left,
        Vector3Int.right,
        Vector3Int.back,
        Vector3Int.forward
    };

    //Meshs Have A Counter Clockwise Order
    private static Vector3[,] Faces =
    {
        { new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(0, 1, 1) }, //Up
            { new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 0, 0) }, //Down
                { new Vector3(0, 0, 1), new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 1) }, //Left
        { new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0) }, //Right
            { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) }, //Front
                { new Vector3(1, 0, 1), new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(1, 1, 1) }, //Back
    };

    private static Vector3[,] SquareFaceOffsets =
{
        { new Vector3(0, 1, 0), new Vector3(1,1,0), new Vector3(1,1,1), new Vector3(0,1,1) }, //Up
            { new Vector3(1, 0, 0), new Vector3(0,0,0), new Vector3(0,0,1), new Vector3(1,0,1) }, //Down
                { new Vector3(0, 0, 1), new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 1) }, //Left
        { new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0) }, //Right
            { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) }, //Front
                { new Vector3(1, 0, 1), new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(1, 1, 1) }, //Back
    };

    private static bool[] FlipFaces =
{
        false,
        true,
        false,
        true,
        false,
        true,
    };

    private static Vector2[] UVs =
    {
        new Vector2(0,0),
        new Vector2(1,0),
        new Vector2(1,1),
        new Vector2(0,1)
    };

    public static MeshUpdateResults CalculateMesh(MeshUpdateRequest request)
    {
        //Make Lists
        List<Vector3> verts = new List<Vector3>();
        List<int> tries = new List<int>();

        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();

        //Loop Through And Form Blocks
        int start = 1;
        int end = request.area + 1;

        //Begin Slicing For Solid Pass
        List<MeshSquare> solidMeshSquares = new List<MeshSquare>();

        SliceAlongX(start, end, request.height, request.voxelData, 4, false, in solidMeshSquares);
        SliceAlongX(start, end, request.height, request.voxelData, 5, false, in solidMeshSquares);

        SliceAlongY(start, end, request.height, request.voxelData, 0, false, in solidMeshSquares);
        SliceAlongY(start, end, request.height, request.voxelData, 1, false, in solidMeshSquares);

        SliceAlongZ(start, end, request.height, request.voxelData, 2, false, in solidMeshSquares);
        SliceAlongZ(start, end, request.height, request.voxelData, 3, false, in solidMeshSquares);
        
        //Add Solid Squares To Mesh
        foreach (MeshSquare square in solidMeshSquares)
        {
            AddFace(in verts, in normals, square);
            AddTriangles(in tries, verts.Count - 1);
            CalculateUVs(in uvs, square.voxelIndex, square.direction);
        }

        ChunkMeshData solid = new ChunkMeshData()
        {
            verticies = verts.ToArray(),
            tries = tries.ToArray(),
            uvs = uvs.ToArray(),
            normals = normals.ToArray()
        };

        verts.Clear();
        tries.Clear();
        uvs.Clear();
        normals.Clear();

        //Begin Slicing For Transparent Pass
        if (request.transparent)
        {
            List<MeshSquare> transparentMeshSquares = new List<MeshSquare>();

            SliceAlongX(start, end, request.height, request.voxelData, 4, true, in transparentMeshSquares);
            SliceAlongX(start, end, request.height, request.voxelData, 5, true, in transparentMeshSquares);

            SliceAlongY(start, end, request.height, request.voxelData, 0, true, in transparentMeshSquares);
            SliceAlongY(start, end, request.height, request.voxelData, 1, true, in transparentMeshSquares);

            SliceAlongZ(start, end, request.height, request.voxelData, 2, true, in transparentMeshSquares);
            SliceAlongZ(start, end, request.height, request.voxelData, 3, true, in transparentMeshSquares);

            //Add Transparent Squares
            foreach (MeshSquare square in transparentMeshSquares)
            {
                AddFace(in verts, in normals, square);
                AddTriangles(in tries, verts.Count - 1);
                CalculateUVs(in uvs, square.voxelIndex, square.direction);
            }

        }

        ChunkMeshData transparent = new ChunkMeshData()
        {
            verticies = verts.ToArray(),
            tries = tries.ToArray(),
            uvs = uvs.ToArray(),
            normals = normals.ToArray()
        };

        //Finish
        return new MeshUpdateResults()
        {
            chunkID = request.chunkID,
            solidPass = solid,
            transparentPass = transparent
        };
    }

    #region Slicing
    //Looping
    private static void SliceAlongX(int start, int end, int height, byte[,,] voxelData, int face, bool trans, in List<MeshSquare> meshSquares)
    {
        int indexX = 0, indexZ = 0;
        List<Vector3Int[]> visitedBounds = new List<Vector3Int[]>();

        for (int z = start; z < end; z++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = start; x < end; x++)
                {
                    //Check Index
                    int voxelIndex = voxelData[x, y, z] - 1;
                    Vector3Int voxelCoords = new Vector3Int(x, y, z);

                    if (voxelIndex == -1 || InVisitedBounds(voxelCoords, visitedBounds))
                    {
                        indexX++;
                        continue;
                    }

                    //Check Tranparency
                    Voxel voxel = VoxelManager.singleton.GetVoxelFromIndex(voxelData[x, y, z]);
                    if (trans != voxel.isTransparent)
                    {
                        indexX++;
                        continue;
                    }

                    //Make Offsets
                    Vector3 position = new Vector3(indexX, y, indexZ);

                    //Check Surounding Voxels
                    int selectedVoxelIndex = TargetVoxel(voxelCoords, face, height, voxelData);

                    Voxel selectedVoxel = null;
                    bool shouldStart = false;

                    if (selectedVoxelIndex != -1)
                    {
                        selectedVoxel = VoxelManager.singleton.GetVoxelFromIndex(selectedVoxelIndex);
                        shouldStart = ShouldPutFace(voxelIndex, selectedVoxel.isTransparent, selectedVoxelIndex, trans);
                    }

                    if ((selectedVoxelIndex == -1 && !selectedVoxel) || (selectedVoxel && shouldStart))
                    {
                        //Define Square
                        MeshSquare ms = new MeshSquare()
                        {
                            voxelIndex = voxelIndex,
                            direction = (WorldConstants.FaceDirections)face,
                            blockForm = voxel.blockForm,

                            bottomLeftCorner = position + SquareFaceOffsets[face, 0],
                            bottomRightCorner = position + SquareFaceOffsets[face, 1],
                            topRightCorner = position + SquareFaceOffsets[face, 2],
                            topLeftCorner = position + SquareFaceOffsets[face, 3],
                        };

                        if (!UseGreedyMeshing)
                        {
                            meshSquares.Add(ms);
                            indexX++;
                            continue;
                        }

                        //Find The X Shift
                        int shiftX = 0;
                        for (int offsetX = 1; offsetX < end - x; offsetX++)
                        {
                            Vector3Int pathFindVoxelCoords = new Vector3Int(offsetX, 0, 0) + voxelCoords;
                            int sV = voxelData[pathFindVoxelCoords.x, pathFindVoxelCoords.y, pathFindVoxelCoords.z] - 1;
                            int sSV = TargetVoxel(pathFindVoxelCoords, face, height, voxelData);

                            if (!InVisitedBounds(pathFindVoxelCoords, visitedBounds))
                            {
                                //Get Voxel
                                bool continueFace = false;
                                Voxel voxelT = null;

                                if (sSV != -1 && sV != -1)
                                {
                                    voxelT = VoxelManager.singleton.GetVoxelFromIndex(sSV);
                                    continueFace = ShouldPutFace(sV, voxelT.isTransparent, sSV, trans);
                                }

                                //Choose Compare Operation
                                if (voxelT == null)
                                {
                                    //Voxel And Air
                                    if (sV != ms.voxelIndex || sSV != -1)
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    //Voxel And Voxel
                                    if (sV != ms.voxelIndex || !continueFace)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                                break;

                            shiftX++;
                        }

                        //Find The Y Shift
                        int shiftY = 0;
                        for (int offsetY = 1; offsetY < height - y; offsetY++)
                        {
                            //Check To See If We Can Even Step
                            Vector3Int yAllign = new Vector3Int(0, offsetY, 0) + voxelCoords;
                            int yV = voxelData[yAllign.x, yAllign.y, yAllign.z] - 1;
                            int ySV = TargetVoxel(yAllign, face, height, voxelData);

                            if (!InVisitedBounds(yAllign, visitedBounds))
                            {
                                //Get Voxel
                                bool continueFace = false;
                                Voxel voxelT = null;

                                if (ySV != -1 && yV != -1)
                                {
                                    voxelT = VoxelManager.singleton.GetVoxelFromIndex(ySV);
                                    continueFace = ShouldPutFace(yV, voxelT.isTransparent, ySV, trans);
                                }

                                //Choose Compare Operation
                                if (voxelT == null)
                                {
                                    if (yV != ms.voxelIndex || ySV != -1)
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    if (yV != ms.voxelIndex || !continueFace)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                                break;

                            //Step Along X Until We Hit Edge
                            int bestXShift = 0;
                            for (int offsetX = 1; offsetX < end - x; offsetX++)
                            {
                                Vector3Int pathFindVoxelCoords = new Vector3Int(offsetX, offsetY, 0) + voxelCoords;
                                int sV = voxelData[pathFindVoxelCoords.x, pathFindVoxelCoords.y, pathFindVoxelCoords.z] - 1;
                                int sSV = TargetVoxel(pathFindVoxelCoords, face, height, voxelData);

                                if (!InVisitedBounds(pathFindVoxelCoords, visitedBounds))
                                {
                                    //Get Voxel
                                    bool continueFace = false;
                                    Voxel voxelT = null;

                                    if (sSV != -1 && sV != -1)
                                    {
                                        voxelT = VoxelManager.singleton.GetVoxelFromIndex(sSV);
                                        continueFace = ShouldPutFace(sV, voxelT.isTransparent, sSV, trans);
                                    }

                                    //Choose Compare Operation
                                    if (voxelT == null)
                                    {
                                        //Voxel And Air
                                        if (sV != ms.voxelIndex || sSV != -1)
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        //Voxel And Voxel
                                        if (sV != ms.voxelIndex || !continueFace)
                                        {
                                            break;
                                        }
                                    }
                                }
                                else
                                    break;


                                bestXShift++;
                            }

                            if (bestXShift != shiftX)
                            {
                                break;
                            }

                            shiftY++;
                        }

                        //Record
                        Vector3Int bestOffsets = new Vector3Int(shiftX, shiftY, 0);
                        visitedBounds.Add(new Vector3Int[2]
                        {
                                    voxelCoords,
                                    new Vector3Int (x + shiftX, y + shiftY, z),
                        });

                        //Add Offsets And Then To Faces
                        if (!FlipFaces[face])
                        {
                            ms.bottomRightCorner += new Vector3(bestOffsets.x, 0, 0);
                            ms.topLeftCorner += new Vector3(0, bestOffsets.y, 0);
                            ms.topRightCorner += new Vector3(bestOffsets.x, bestOffsets.y, 0);
                        }
                        else
                        {
                            ms.bottomLeftCorner += new Vector3(bestOffsets.x, 0, 0);
                            ms.topLeftCorner += new Vector3(bestOffsets.x, bestOffsets.y, 0);
                            ms.topRightCorner += new Vector3(0, bestOffsets.y, 0);
                        }

                        //Add To Faces
                        meshSquares.Add(ms);
                    }

                    indexX++;
                }

                indexX = 0;
            }

            visitedBounds.Clear();
            indexX = 0;
            indexZ++;
        }
    }

    private static void SliceAlongY(int start, int end, int height, byte[,,] voxelData, int face, bool trans, in List<MeshSquare> meshSquares)
    {
        int indexX = 0, indexZ = 0;
        List<Vector3Int[]> visitedBounds = new List<Vector3Int[]>();

        for (int y = 0; y < height; y++)
        {
            for (int z = start; z < end; z++)
            {
                for (int x = start; x < end; x++)
                {
                    //Check Index
                    int voxelIndex = voxelData[x, y, z] - 1;
                    Vector3Int voxelCoords = new Vector3Int(x, y, z);

                    if (voxelIndex == -1 || (InVisitedBounds(voxelCoords, visitedBounds)))
                    {
                        indexX++;
                        continue;
                    }

                    //Check Tranparency
                    Voxel voxel = VoxelManager.singleton.GetVoxelFromIndex(voxelData[x, y, z]);
                    if (trans != voxel.isTransparent)
                    {
                        indexX++;
                        continue;
                    }

                    //Make Offsets
                    Vector3 position = new Vector3(indexX, y, indexZ);

                    //Check Surounding Voxels
                    int selectedVoxelIndex = TargetVoxel(voxelCoords, face, height, voxelData);

                    Voxel selectedVoxel = null;
                    bool shouldStart = false;

                    if (selectedVoxelIndex != -1)
                    {
                        selectedVoxel = VoxelManager.singleton.GetVoxelFromIndex(selectedVoxelIndex);
                        shouldStart = ShouldPutFace(voxelIndex, selectedVoxel.isTransparent, selectedVoxelIndex, trans);
                    }

                    if ((selectedVoxelIndex == -1 && !selectedVoxel) || (selectedVoxel && shouldStart))
                    {
                        MeshSquare ms = new MeshSquare()
                        {
                            voxelIndex = voxelIndex,
                            direction = (WorldConstants.FaceDirections)face,
                            blockForm = voxel.blockForm,

                            bottomLeftCorner = position + SquareFaceOffsets[face, 0],
                            bottomRightCorner = position + SquareFaceOffsets[face, 1],
                            topRightCorner = position + SquareFaceOffsets[face, 2],
                            topLeftCorner = position + SquareFaceOffsets[face, 3],
                        };

                        if (!UseGreedyMeshing)
                        {
                            meshSquares.Add(ms);
                            indexX++;
                            continue;
                        }

                        //Find The X Shift

                        int shiftX = 0;
                        for (int offsetX = 1; offsetX < end - x; offsetX++)
                        {
                            Vector3Int pathFindVoxelCoords = new Vector3Int(offsetX, 0, 0) + voxelCoords;
                            int sV = voxelData[pathFindVoxelCoords.x, pathFindVoxelCoords.y, pathFindVoxelCoords.z] - 1;
                            int sSV = TargetVoxel(pathFindVoxelCoords, face, height, voxelData);

                            if (!InVisitedBounds(pathFindVoxelCoords, visitedBounds))
                            {
                                //Get Voxel
                                bool continueFace = false;
                                Voxel voxelT = null;

                                if(sSV != -1 && sV != -1)
                                {
                                    voxelT = VoxelManager.singleton.GetVoxelFromIndex(sSV);
                                    continueFace = ShouldPutFace(sV, voxelT.isTransparent, sSV, trans);
                                }

                                //Choose Compare Operation
                                if (voxelT == null)
                                {
                                    //Voxel And Air
                                    if (sV != ms.voxelIndex || sSV != -1)
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    //Voxel And Voxel
                                    if(sV != ms.voxelIndex || !continueFace)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                                break;

                            shiftX++;
                        }

                        //Find The Z Shift
                        int shiftZ = 0;
                        for (int offsetZ = 1; offsetZ < end - z; offsetZ++)
                        {
                            //Try To Step Along Z
                            Vector3Int zAllign = new Vector3Int(0, 0, offsetZ) + voxelCoords;
                            int zV = voxelData[zAllign.x, zAllign.y, zAllign.z] - 1;
                            int zSV = TargetVoxel(zAllign, face, height, voxelData);


                            if (!InVisitedBounds(zAllign, visitedBounds))
                            {
                                //Get Voxel
                                bool continueFace = false;
                                Voxel voxelT = null;

                                if (zSV != -1 && zV != -1)
                                {
                                    voxelT = VoxelManager.singleton.GetVoxelFromIndex(zSV);
                                    continueFace = ShouldPutFace(zV, voxelT.isTransparent, zSV, trans);
                                }

                                //Choose Compare Operation
                                if (voxelT == null)
                                { 
                                    if (zV != ms.voxelIndex || zSV != -1)
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    if (zV != ms.voxelIndex || !continueFace)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                                break;

                            //Step Along X Until Break
                            int bestXShift = 0;
                            for (int offsetX = 1; offsetX < end - x; offsetX++)
                            {
                                Vector3Int pathFindVoxelCoords = new Vector3Int(offsetX, 0, offsetZ) + voxelCoords;
                                int sV = voxelData[pathFindVoxelCoords.x, pathFindVoxelCoords.y, pathFindVoxelCoords.z] - 1;
                                int sSV = TargetVoxel(pathFindVoxelCoords, face, height, voxelData);

                                if (!InVisitedBounds(pathFindVoxelCoords, visitedBounds))
                                {
                                    //Get Voxel
                                    bool continueFace = false;
                                    Voxel voxelT = null;

                                    if (sSV != -1 && sV != -1)
                                    {
                                        voxelT = VoxelManager.singleton.GetVoxelFromIndex(sSV);
                                        continueFace = ShouldPutFace(sV, voxelT.isTransparent, sSV, trans);
                                    }

                                    //Choose Compare Operation
                                    if (voxelT == null)
                                    {
                                        //Voxel And Air
                                        if (sV != ms.voxelIndex || sSV != -1)
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        //Voxel And Voxel
                                        if (sV != ms.voxelIndex || !continueFace)
                                        {
                                            break;
                                        }
                                    }
                                }
                                else
                                    break;

                                bestXShift++;
                            }

                            if (bestXShift != shiftX)
                                break;

                            shiftZ++;
                        }
                        

                        //Record
                        Vector3Int bestOffsets = new Vector3Int(shiftX, 0, shiftZ);

                        visitedBounds.Add(new Vector3Int[2]
                            {
                                voxelCoords,
                                new Vector3Int (x + shiftX, y, z + shiftZ),
                            });

                        //Add Offsets And Then To Faces
                        if (!FlipFaces[face])
                        {
                            ms.bottomRightCorner += new Vector3(bestOffsets.x, 0, 0);
                            ms.topRightCorner += new Vector3(bestOffsets.x, 0, bestOffsets.z);
                            ms.topLeftCorner += new Vector3(0, 0, bestOffsets.z);
                        }
                        else
                        {
                            ms.bottomLeftCorner += new Vector3(bestOffsets.x, 0, 0);
                            ms.topRightCorner += new Vector3(0, 0, bestOffsets.z);
                            ms.topLeftCorner += new Vector3(bestOffsets.x, 0, bestOffsets.z);
                        }

                        meshSquares.Add(ms);
                    }


                    indexX++;
                }

                indexX = 0;
                indexZ++;
            }

            visitedBounds.Clear();
            indexX = 0;
            indexZ = 0;
        }
    }

    private static void SliceAlongZ(int start, int end, int height, byte[,,] voxelData, int face, bool trans, in List<MeshSquare> meshSquares)
    {
        int indexX = 0, indexZ = 0;
        List<Vector3Int[]> visitedBounds = new List<Vector3Int[]>();

        for (int x = start; x < end; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = start; z < end; z++)
                {
                    //Check Index
                    int voxelIndex = voxelData[x, y, z] - 1;
                    Vector3Int voxelCoords = new Vector3Int(x, y, z);

                    if (voxelIndex == -1 || InVisitedBounds(voxelCoords, visitedBounds))
                    {
                        indexZ++;
                        continue;
                    }

                    //Check Tranparency
                    Voxel voxel = VoxelManager.singleton.GetVoxelFromIndex(voxelData[x, y, z]);
                    if (trans != voxel.isTransparent)
                    {
                        indexZ++;
                        continue;
                    }

                    //Make Offsets
                    Vector3 position = new Vector3(indexX, y, indexZ);

                    //Check Surounding Voxels
                    int selectedVoxelIndex = TargetVoxel(voxelCoords, face, height, voxelData);

                    Voxel selectedVoxel = null;
                    bool shouldStart = false;

                    if (selectedVoxelIndex != -1)
                    {
                        selectedVoxel = VoxelManager.singleton.GetVoxelFromIndex(selectedVoxelIndex);
                        shouldStart = ShouldPutFace(voxelIndex, selectedVoxel.isTransparent, selectedVoxelIndex, trans);
                    }

                    if ((selectedVoxelIndex == -1 && !selectedVoxel) || (selectedVoxel && shouldStart))
                    {
                        MeshSquare ms = new MeshSquare()
                        {
                            voxelIndex = voxelIndex,
                            direction = (WorldConstants.FaceDirections)face,
                            blockForm = voxel.blockForm,

                            bottomLeftCorner = position + SquareFaceOffsets[face, 0],
                            bottomRightCorner = position + SquareFaceOffsets[face, 1],
                            topRightCorner = position + SquareFaceOffsets[face, 2],
                            topLeftCorner = position + SquareFaceOffsets[face, 3],
                        };

                        if (!UseGreedyMeshing)
                        {
                            meshSquares.Add(ms);
                            indexZ++;
                            continue;
                        }

                        //Find The Z Shift
                        int shiftZ = 0;
                        for (int offsetZ = 1; offsetZ < end - z; offsetZ++)
                        {
                            Vector3Int pathFindVoxelCoords = new Vector3Int(0, 0, offsetZ) + voxelCoords;
                            int sV = voxelData[pathFindVoxelCoords.x, pathFindVoxelCoords.y, pathFindVoxelCoords.z] - 1;
                            int sSV = TargetVoxel(pathFindVoxelCoords, face, height, voxelData);

                            if (!InVisitedBounds(pathFindVoxelCoords, visitedBounds))
                            {
                                //Get Voxel
                                bool continueFace = false;
                                Voxel voxelT = null;

                                if (sSV != -1 && sV != -1)
                                {
                                    voxelT = VoxelManager.singleton.GetVoxelFromIndex(sSV);
                                    continueFace = ShouldPutFace(sV, voxelT.isTransparent, sSV, trans);
                                }

                                //Choose Compare Operation
                                if (voxelT == null)
                                {
                                    //Voxel And Air
                                    if (sV != ms.voxelIndex || sSV != -1)
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    //Voxel And Voxel
                                    if (sV != ms.voxelIndex || !continueFace)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                                break;

                            shiftZ++;
                        }


                        //Find The Y Shift
                        int shiftY = 0;
                        for (int offsetY = 1; offsetY < height - y; offsetY++)
                        {
                            Vector3Int yAllign = new Vector3Int(0, offsetY, 0) + voxelCoords;
                            int yV = voxelData[yAllign.x, yAllign.y, yAllign.z] - 1;
                            int ySV = TargetVoxel(yAllign, face, height, voxelData);
                            if (!InVisitedBounds(yAllign, visitedBounds))
                            {
                                //Get Voxel
                                bool continueFace = false;
                                Voxel voxelT = null;

                                if (ySV != -1 && yV != -1)
                                {
                                    voxelT = VoxelManager.singleton.GetVoxelFromIndex(ySV);
                                    continueFace = ShouldPutFace(yV, voxelT.isTransparent, ySV, trans);
                                }

                                //Choose Compare Operation
                                if (voxelT == null)
                                {
                                    //Voxel And Air
                                    if (yV != ms.voxelIndex || ySV != -1)
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    //Voxel And Voxel
                                    if (yV != ms.voxelIndex || !continueFace)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                                break;

                            int bestZShift = 0;
                            for (int offsetZ = 1; offsetZ < end - z; offsetZ++)
                            {
                                Vector3Int pathFindVoxelCoords = new Vector3Int(0, offsetY, offsetZ) + voxelCoords;
                                int sV = voxelData[pathFindVoxelCoords.x, pathFindVoxelCoords.y, pathFindVoxelCoords.z] - 1;
                                int sSV = TargetVoxel(pathFindVoxelCoords, face, height, voxelData);

                                if (!InVisitedBounds(pathFindVoxelCoords, visitedBounds))
                                {
                                    //Get Voxel
                                    bool continueFace = false;
                                    Voxel voxelT = null;

                                    if (sSV != -1 && sV != -1)
                                    {
                                        voxelT = VoxelManager.singleton.GetVoxelFromIndex(sSV);
                                        continueFace = ShouldPutFace(sV, voxelT.isTransparent, sSV, trans);
                                    }

                                    //Choose Compare Operation
                                    if (voxelT == null)
                                    {
                                        //Voxel And Air
                                        if (sV != ms.voxelIndex || sSV != -1)
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        //Voxel And Voxel
                                        if (sV != ms.voxelIndex || !continueFace)
                                        {
                                            break;
                                        }
                                    }
                                }
                                else
                                    break;

                                bestZShift++;
                            }

                            if (bestZShift != shiftZ)
                                break;

                            shiftY++;
                        }

                        //Record
                        Vector3Int bestOffsets = new Vector3Int(0, shiftY, shiftZ);

                        visitedBounds.Add(new Vector3Int[2]
                        {
                                    voxelCoords,
                                    new Vector3Int (x, y + shiftY, z + shiftZ),
                        });

                        //Add Offsets And Then To Faces
                        if (!FlipFaces[face])
                        {
                            ms.bottomLeftCorner += new Vector3(0, 0, bestOffsets.z);
                            ms.topLeftCorner += new Vector3(0, bestOffsets.y, bestOffsets.z);
                            ms.topRightCorner += new Vector3(0, bestOffsets.y, 0);
                        }
                        else
                        {
                            ms.bottomRightCorner += new Vector3(0, 0, bestOffsets.z);
                            ms.topLeftCorner += new Vector3(0, bestOffsets.y, 0);
                            ms.topRightCorner += new Vector3(0, bestOffsets.y, bestOffsets.z);
                        }

                        //Add To Faces
                        meshSquares.Add(ms);
                    }
                    indexZ++;
                }
                indexZ=0;
            }

            indexZ = 0;
            indexX++;
        }
    }
    #endregion

    //Slice Helpers
    private static int TargetVoxel(Vector3Int voxelCoords, int coord, int hight, byte[,,] voxelData)
    {
        int si = -1;

        //Find Offsets
        Vector3Int targetVoxelCoords = voxelCoords + CoordOffsets[coord];

        //Check Vertical
        if (targetVoxelCoords.y >= 0 && targetVoxelCoords.y < hight)
        {
            //Check Horizontal
            si = voxelData[targetVoxelCoords.x, targetVoxelCoords.y, targetVoxelCoords.z] - 1;
        }

        return si;
    }

    private static bool InVisitedBounds(Vector3 point, List<Vector3Int[]> visitedBounds)
    {
        for (int i = 0; i < visitedBounds.Count; i++)
        {
            Vector3Int a = visitedBounds[i][0];
            Vector3Int b = visitedBounds[i][1];

            bool x = point.x >= a.x && point.x <= b.x;
            bool y = point.y >= a.y && point.y <= b.y;
            bool z = point.z >= a.z && point.z <= b.z;

            if (x && y && z)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldPutFace(int voxel, bool targetT, int targetV, bool shouldBeT)
    {
        //This Function is only called when two voxels are being compared and not air
        //return true for face and false for no face
        //If we should be transparent and the target is transparent then return false
        //if we should be transparent and the target is air then return true
        //if we should not be transparent and the is transparent then return true

        if (shouldBeT)
        {
            if (!targetT)
            {
                return false;
            }

            if (targetT && voxel == targetV)
            {
                return false;
            }

            return true;
        }
        else
        {
            //If target is transparent
            if (targetT)
            {
                return true;
            }

            return false;
        }
    }

    //Add Face
    private static void AddFace(in List<Vector3> verts, in List<Vector3> normals, MeshSquare square)
    {
        verts.Add(square.bottomLeftCorner);
        verts.Add(square.bottomRightCorner);
        verts.Add(square.topRightCorner);
        verts.Add(square.topLeftCorner);

        normals.Add(CoordOffsets[(int)square.direction]);
        normals.Add(CoordOffsets[(int)square.direction]);
        normals.Add(CoordOffsets[(int)square.direction]);
        normals.Add(CoordOffsets[(int)square.direction]);
    }

    //Add Triangles
    private static void AddTriangles(in List<int> tries, int count)
    {
        tries.Add(count - 0);
        tries.Add(count - 1);
        tries.Add(count - 2);

        tries.Add(count - 2);
        tries.Add(count - 3);
        tries.Add(count - 0);
    }

    //Get UVs
    private static void CalculateUVs(in List<Vector2> uvs, int voxelIndex, WorldConstants.FaceDirections faceDirections)
    {
        Vector2 c = VoxelManager.singleton.SampleAtlas(voxelIndex, faceDirections);

        //uvs.Add(c[0]);
        //uvs.Add(c[1]);
        //uvs.Add(c[2]);
        //uvs.Add(c[3]);

        uvs.Add(c);
        uvs.Add(c);
        uvs.Add(c);
        uvs.Add(c);

    }

    //Square Data
    public class MeshSquare
    {
        public Vector3 bottomLeftCorner;
        public Vector3 bottomRightCorner;
        public Vector3 topRightCorner;
        public Vector3 topLeftCorner;
        public int voxelIndex;
        public WorldConstants.FaceDirections direction;
        public WorldConstants.BlockForm blockForm;
    }
}