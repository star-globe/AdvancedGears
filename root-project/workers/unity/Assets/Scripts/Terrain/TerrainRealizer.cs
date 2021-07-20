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

        [SerializeField]
        int seeds;

        private void Start()
        {
            Realize(FieldTemplate.CreateTerrainPointInfo(range, highest));
        }

        public void Realize(List<TerrainPointInfo> terrainPoints)
        {
            var width = terrain.terrainData.heightmapResolution;
            var height = terrain.terrainData.heightmapResolution;

            var size = terrain.terrainData.size;

            Debug.LogFormat("width:{0} height{1}", width, height);
            var pos = transform.position;

            float[,] heights = new float[width, width];

            foreach (var point in terrainPoints)
            {
                var p = point;
                p.Seeds = seeds;
                p.SetHeights(center, pos.x, pos.z, width, height, size, heights);
            }

            terrain.terrainData.SetHeights(0,0, heights);
        }
    }
}
