using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelEditor : MonoBehaviour
{
    [SerializeField] private float maxDistance;
    [SerializeField] private LayerMask world;
    [Space]
    [SerializeField] private GameObject targetPrefab;

    PlayerContoller contoller;
    bool hasBlock = false;
    GameObject target;

    Vector3 point = Vector3.zero;
    Vector3 normal = Vector3.zero;

    //Voxels
    int currentSelectedVoxel = 0;
    int oldSelectedVoxel = 0;


    //Setup
    private void Start()
    {
        contoller = GetComponent<PlayerContoller>();
        hasBlock = false;

        point = Vector3.zero;
        normal = Vector3.zero;

        currentSelectedVoxel = 0;
        oldSelectedVoxel = 0;
    }

    void Update()
    {
        GetTargetBlock();
        HandleTargetObject();

        //Stop For Loading
        if (WorldGenerationManager.singleton.GenerationState == WorldGenerationManager.WorldState.Idle)
            return;

        if (!contoller.isAbleToEdit())
            return;

        //Run Through Voxel Selection
        ChangeSelectedVoxel();

        //Check For Block
        if (!hasBlock) 
            return;

        RunVoxelEdits();
    }

    //Get Block Target
    void GetTargetBlock()
    {
        if (!contoller.isAbleToEdit())
        {
            hasBlock = false;
            return;
        }


        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, world))
        {
            hasBlock = true;
            point = hit.point;
            normal = hit.normal;
        }
        else
            hasBlock = false;
    }

    //Update Target Indecator
    void HandleTargetObject()
    {
        //Update Target Block
        if (hasBlock)
        {
            float offset = .2f;
            Vector3 p = point - new Vector3(offset * normal.x, offset * normal.y, offset * normal.z);
            Vector3Int vp = WorldGenerationManager.singleton.VoxelLocationInWorld(p);

            Vector3 center = vp + (Vector3.one / 2f);
            Vector3 sidePosition = center + new Vector3(.5001f * normal.x, .5001f * normal.y, .5001f * normal.z);

            if (target == null)
            {
                target = Instantiate(targetPrefab, sidePosition, Quaternion.identity);
                target.transform.rotation = Quaternion.LookRotation(-normal);
            }
            else
            {
                target.transform.position = sidePosition;
                target.transform.rotation = Quaternion.LookRotation(-normal);
            }
        }
        else
        {
            if (target != null)
            {
                Destroy(target);
            }
        }
    }

    //Choose A Voxel To Place
    private void ChangeSelectedVoxel()
    {
        int minVoxelIndex = 0;
        int maxVoxelIndex = 6;

        //Scroll Input
        if(Input.mouseScrollDelta.y > 0) // Up
        {
            currentSelectedVoxel++;
            if (currentSelectedVoxel > maxVoxelIndex)
                currentSelectedVoxel = minVoxelIndex;
        }
        else if (Input.mouseScrollDelta.y < 0) // Down
        {
            currentSelectedVoxel--;
            if (currentSelectedVoxel < minVoxelIndex)
                currentSelectedVoxel = maxVoxelIndex;
        }

        //Number Input
        if (Input.GetKeyDown(KeyCode.Alpha1))
            currentSelectedVoxel = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            currentSelectedVoxel = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            currentSelectedVoxel = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            currentSelectedVoxel = 3;
        else if (Input.GetKeyDown(KeyCode.Alpha5))
            currentSelectedVoxel = 4;
        else if (Input.GetKeyDown(KeyCode.Alpha6))
            currentSelectedVoxel = 5;
        else if (Input.GetKeyDown(KeyCode.Alpha7))
            currentSelectedVoxel = 6;

        //Update UI
        if (oldSelectedVoxel != currentSelectedVoxel)
        {
            oldSelectedVoxel = currentSelectedVoxel;
            GameplayManager.singleton.CallChangedVoxel(currentSelectedVoxel);
        }
    }

    //Handle Voxel Edits
    private void RunVoxelEdits()
    {
        //Break Blocks
        if (Input.GetMouseButtonDown(0))
        {
            float offset = .2f;
            Vector3 p = point - new Vector3(offset * normal.x, offset * normal.y, offset * normal.z);

            //Run Logic
            Vector3Int vp = WorldGenerationManager.singleton.VoxelLocationInWorld(p);
            int voxel = WorldGenerationManager.singleton.GetVoxelAtPoint(vp);

            if (voxel != -1)
            {
                if (!VoxelManager.singleton.voxels[voxel].isUnbreakable)
                {
                    //Start Request
                    WorldGenerationManager.singleton.EditVoxelsRequest(vp, -1);
                }
            }
        }

        //Place Blocks
        if (Input.GetMouseButtonDown(1))
        {
            float offset = .2f;
            Vector3 p = point + new Vector3(offset * normal.x, offset * normal.y, offset * normal.z);

            //Run Logic
            Vector3Int vp = WorldGenerationManager.singleton.VoxelLocationInWorld(p);
            int voxel = WorldGenerationManager.singleton.GetVoxelAtPoint(vp);

            //Check For Player Intersection
            Vector3Int lower = WorldGenerationManager.singleton.VoxelLocationInWorld(transform.position + new Vector3(0, -offset, 0));
            Vector3Int upper = WorldGenerationManager.singleton.VoxelLocationInWorld(transform.position + new Vector3(0, offset, 0));

            if ((vp == lower || vp == upper) && voxel != 4)
            {
                return;
            }

            //Update
            if (voxel == -1 || voxel == 4)
            {
                //Start Request
                WorldGenerationManager.singleton.EditVoxelsRequest(vp, currentSelectedVoxel);
            }
        }
    }
}
