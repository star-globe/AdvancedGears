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
            Realize(FieldTemplate.CreateFieldEntityTemplate(range, highest));
        }

        public void Realize(List<TerrainPointInfo> settings)
        {
            var width = terrain.terrainData.heightmapWidth;
            var pos = transform.position;

            float[,] heights = new float[width, width];

            foreach (var point in terrainsPoints)
                heights = point.SetHeights(center, pos.x, pos.z, width, heights);

            terrain.terrainData.SetHeights(0,0, heights);
        }
    }
}
