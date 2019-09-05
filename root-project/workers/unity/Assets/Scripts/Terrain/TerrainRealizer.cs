using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class TerrainRealizer : MonoBehaviour
    {
        [SerializeField]
        Terrain terrain;

        [SerializeField]
        float range;
        
        [SerializeField]
        float highest;

        [SerializeField]
        Vector3 center;

        private void Start()
        {
            Realize(FieldTemplate.CreateTerrainPointInfo(range, highest));
        }

        public void Realize(List<TerrainPointInfo> terrainPoints)
        {
            var width = terrain.terrainData.heightmapWidth;
            var height = terrain.terrainData.heightmapHeight;

            var size = terrain.terrainData.size;

            Debug.LogFormat("width:{0} height{1}", width, height);
            var pos = transform.position;

            float[,] heights = new float[width, width];

            //foreach (var point in terrainPoints)
            //    heights = point.SetHeights(center, pos.x, pos.z, width, height, size, heights);

            for (int i = 0; i < width; i++)
            {
                for (int k = 0; k < width; k++)
                {
                    heights[i, k] = ((pos.x + 1000)/4 +i + k * 0.5f) / 1024;
                }
            }

            terrain.terrainData.SetHeights(0,0, heights);
        }
    }
}
