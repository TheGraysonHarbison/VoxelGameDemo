using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VoxelBar : MonoBehaviour
{
    [SerializeField] private Color restColor;
    [SerializeField] private Color activeColor;
    [Space]
    [SerializeField] private Image[] slots;

    int selectedVoxel = 0;

    private void Awake()
    {
        GameplayManager.SelectsVoxel += ChangeVoxel;
        selectedVoxel = 0;
        UpdateUI();
    }

    private void ChangeVoxel(int v)
    {
        selectedVoxel = v;
        UpdateUI();
    }

    private void UpdateUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i == selectedVoxel)
            {
                slots[i].color = activeColor;
            }
            else
            {
                slots[i].color = restColor;
            }
        }
    }

    private void OnDestroy()
    {
        GameplayManager.SelectsVoxel -= ChangeVoxel;
    }
}
