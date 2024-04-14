using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VoxelManager))]
public class VoxelManagerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        VoxelManager vm = (VoxelManager)target;

        if(GUILayout.Button("Sort Voxels"))
        {
            vm.SortVoxels();
        }

        if(GUILayout.Button("Regenerate Atlas"))
        {
            vm.GenerateAtlas(true);
        }

        base.OnInspectorGUI();
    }
}
