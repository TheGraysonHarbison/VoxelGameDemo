using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldGenerationSettings))]
public class WorldSettingsInspector : Editor
{
    const int SIZE = 128;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        //Get Target
        WorldGenerationSettings settings = (WorldGenerationSettings)target;

        if (settings.displayType == WorldGenerationSettings.DisplayDataType.None)
        {
            return;
        }
        else if(settings.displayType == WorldGenerationSettings.DisplayDataType.Masks)
        {
            DisplayMasks(settings);
        }
        else if (settings.displayType == WorldGenerationSettings.DisplayDataType.Biomes)
        {
            DisplayBiomes(settings);
        }
        else if (settings.displayType == WorldGenerationSettings.DisplayDataType.Elevation)
        {
            DisplayElevation(settings);
        }
        else if (settings.displayType == WorldGenerationSettings.DisplayDataType.Erosion)
        {
            DisplayErosion(settings);
        }
        else if (settings.displayType == WorldGenerationSettings.DisplayDataType.Peaks)
        {
            DisplayPeaks(settings);
        }
        else if (settings.displayType == WorldGenerationSettings.DisplayDataType.Trees)
        {
            DisplayTrees(settings);
        }
        else
        {
            DisplayWorld(settings);
        }
    }

    private void DisplayMasks(WorldGenerationSettings settings)
    {
        //Make Noise Samplers
        NoiseSampler tempuratureSampler = new NoiseSampler(12, settings.TemperatureNoiseSettings);
        NoiseSampler moistureSampler = new NoiseSampler(12, settings.MoistureNoiseSettings);

        //Fill Texture
        Texture2D debugTex = new Texture2D(SIZE, SIZE);
        debugTex.filterMode = FilterMode.Point;

        for (int x = 0; x < SIZE; x++)
        {
            for (int y = 0; y < SIZE; y++)
            {
                float sampleT = tempuratureSampler.Sample2D(new Vector3(x, y, 0));
                float sampleM = moistureSampler.Sample2D(new Vector3(x, y, 0));

                float t = Mathf.InverseLerp(-1, 1, sampleT);
                float m = Mathf.InverseLerp(-1, 1, sampleM);
                debugTex.SetPixel(x, y, new Color(t, m, 0, 1));
            }
        }

        debugTex.Apply();

        GUILayout.Label(debugTex);
    }

    private void DisplayBiomes(WorldGenerationSettings settings)
    {
        //Make Noise Samplers
        NoiseSampler tempuratureSampler = new NoiseSampler(12, settings.TemperatureNoiseSettings);
        NoiseSampler moistureSampler = new NoiseSampler(12, settings.MoistureNoiseSettings);

        //Fill Texture
        Texture2D debugTex = new Texture2D(SIZE, SIZE);
        debugTex.filterMode = FilterMode.Point;

        for (int x = 0; x < SIZE; x++)
        {
            for (int y = 0; y < SIZE; y++)
            {
                float sampleT = tempuratureSampler.Sample2D(new Vector3(x, y, 0)) * 2f;
                float sampleM = moistureSampler.Sample2D(new Vector3(x, y, 0)) * 2f;

                Color c = CalculateBiomeAtPoint(settings, sampleT, sampleM);
                debugTex.SetPixel(x, y, c);
            }
        }

        debugTex.Apply();

        GUILayout.Label(debugTex);
    }

    public Color CalculateBiomeAtPoint(WorldGenerationSettings settings, float t, float m)
    {
        //Check All Biomes
        BiomeSettings[] biomes = settings.Biomes;
        int bestBiomeIndex = 0;

        //Get Best
        float BestDistance = float.MaxValue;

        for (int i = 0; i < biomes.Length; i++)
        {
            float centerX = biomes[i].MinTemperature + ((biomes[i].MaxTemperature - biomes[i].MinTemperature) / 2f);
            float centerY = biomes[i].MinMoisture + ((biomes[i].MaxMoisture - biomes[i].MinMoisture) / 2f);
            Vector2 target = new Vector2(centerX, centerY);

            float dst = Vector2.Distance(new Vector2(t, m), target);
            if (dst <= BestDistance)
            {
                BestDistance = dst;
                bestBiomeIndex = i;
            }
        }



        return biomes[bestBiomeIndex].Color;
    }


    float Distance(float a, float b)
    {
        float s = Mathf.Abs(a * a) * Mathf.Abs(b * b);
        return Mathf.Sqrt(s);
    }

    private void DisplayElevation(WorldGenerationSettings settings)
    {
        //Make Noise Samplers
        NoiseSampler elevationSampler = new NoiseSampler(12, settings.ElevationNoiseSettings);

        //Fill Texture
        Texture2D debugTex = new Texture2D(SIZE, SIZE);
        debugTex.filterMode = FilterMode.Point;

        for (int x = 0; x < SIZE; x++)
        {
            for (int y = 0; y < SIZE; y++)
            {
                float sampleT = elevationSampler.Sample2D(new Vector3(x, y, 0));
                float t = Mathf.InverseLerp(-1, 1, sampleT);

                debugTex.SetPixel(x, y, new Color(t, t, t, 1));
            }
        }

        debugTex.Apply();

        GUILayout.Label(debugTex);
    }

    private void DisplayErosion(WorldGenerationSettings settings)
    {
        //Make Noise Samplers
        NoiseSampler erosionSampler = new NoiseSampler(12, settings.ErosionNoiseSettings);

        //Fill Texture
        Texture2D debugTex = new Texture2D(SIZE, SIZE);
        debugTex.filterMode = FilterMode.Point;

        for (int x = 0; x < SIZE; x++)
        {
            for (int y = 0; y < SIZE; y++)
            {
                float sampleT = erosionSampler.Sample2D(new Vector3(x, y, 0));
                float t = Mathf.InverseLerp(-1, 1, sampleT);

                debugTex.SetPixel(x, y, new Color(t, t, t, 1));
            }
        }

        debugTex.Apply();

        GUILayout.Label(debugTex);
    }

    private void DisplayPeaks(WorldGenerationSettings settings)
    {
        //Make Noise Samplers
        NoiseSampler peaksSampler = new NoiseSampler(12, settings.PeaksNoiseSettings);

        //Fill Texture
        Texture2D debugTex = new Texture2D(SIZE, SIZE);
        debugTex.filterMode = FilterMode.Point;

        for (int x = 0; x < SIZE; x++)
        {
            for (int y = 0; y < SIZE; y++)
            {
                float sampleT = peaksSampler.Sample2D(new Vector3(x, y, 0));
                float t = Mathf.InverseLerp(-1, 1, sampleT);
                debugTex.SetPixel(x, y, new Color(t, t, t, 1));
            }
        }

        debugTex.Apply();

        GUILayout.Label(debugTex);
    }

    private void DisplayTrees(WorldGenerationSettings settings)
    {
        //Make Noise Samplers


        //Fill Texture
        Texture2D debugTex = new Texture2D(16, 16);
        debugTex.filterMode = FilterMode.Point;

        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 16; y++)
            {

            }
        }

        debugTex.Apply();

        GUILayout.Label(debugTex);
    }

    private void DisplayWorld(WorldGenerationSettings settings)
    {
        //Make Noise Samplers
        NoiseSampler elevationSampler = new NoiseSampler(12, settings.ElevationNoiseSettings);
        NoiseSampler erosionSampler = new NoiseSampler(12, settings.ErosionNoiseSettings);
        NoiseSampler peaksSampler = new NoiseSampler(12, settings.PeaksNoiseSettings);

        //Fill Texture
        Texture2D debugTex = new Texture2D(SIZE, SIZE);
        debugTex.filterMode = FilterMode.Point;

        for (int x = 0; x < SIZE; x++)
        {
            for (int y = 0; y < SIZE; y++)
            {
                float sampleEL = elevationSampler.Sample2D(new Vector3(x, y, 0));
                float sampleER = erosionSampler.Sample2D(new Vector3(x, y, 0));
                float sampleT = peaksSampler.Sample2D(new Vector3(x, y, 0));

                sampleEL = Mathf.InverseLerp(-1, 1, sampleEL);

                sampleER = Mathf.InverseLerp(-1, 1, sampleER);

                sampleT = Mathf.InverseLerp(-1, 1, sampleT);

                float t = (sampleEL + sampleT) * sampleER;

                debugTex.SetPixel(x, y, new Color(t, t, t, 1));
            }
        }

        debugTex.Apply();

        GUILayout.Label(debugTex);
    }
}
