using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class PerlinNoise : MonoBehaviour
    {
        [Serializable]
        class PerlinSettings
        {
            [SerializeField]
            int HighestHillHeight;

            [SerializeField]
            int LowestHillHeight;

            [SerializeField]
            float tileSize;

            [SerializeField]
            int seeds = 1;

            public float[,] SetHeights(int width, int height, float[,] b_heights)
            {
                float hillHeight = (float)((float)HighestHillHeight - (float)LowestHillHeight) / ((float)height / 2);
                float baseHeight = (float)LowestHillHeight / ((float)height / 2);

                float[,] heights = new float[width, height];
                for (int i = 0; i < width; i++)
                {
                    for (int k = 0; k < height; k++)
                    {
                        heights[i, k] = baseHeight + b_heights[i, k] + (Mathf.PerlinNoise(seeds + ((float)i / (float)width) * tileSize, seeds + ((float)k / (float)height) * tileSize) * (float)hillHeight);
                    }
                }

                return heights;
            }
        }

        [SerializeField]
        Terrain terrain;

        [SerializeField]
        List<PerlinSettings> settings = new List<PerlinSettings>();

        private void Start()
        {
            var width = terrain.terrainData.heightmapWidth;
            var height = terrain.terrainData.heightmapHeight;

            float[,] heights = new float[width, height];

            foreach (var set in settings)
            {
                heights = set.SetHeights(width, height, heights);
            }

            terrain.terrainData.SetHeights(0,0, heights);
        }
    }

}
