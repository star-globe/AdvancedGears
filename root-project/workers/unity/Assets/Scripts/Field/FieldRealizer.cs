using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class FieldRealizer : MonoBehaviour
    {
        [SerializeField]
        Terrain terrain;

        private void Start()
        {
            Assert.IsNotNull(terrain);
        }

        public void Realize(List<TerrainPointInfo> terrainPoints, Vector3 pos)
        {
            this.transform.position = pos;

            var width = terrain.terrainData.heightmapWidth;
            var height = terrain.terrainData.heightmapHeight;
            var size = terrain.terrainData.size;

            float[,] heights = new float[width, width];

            //foreach (var point in terrainPoints)
            //    heights = point.SetHeights(pos, pos.x, pos.z, width, height, size, heights);
            for (int i = 0; i < width; i++) {
                for (int k = 0; k < width; k++) {
                    heights[i, k] = (i + k) * 0.1f;
                }
            }

            terrain.terrainData.SetHeights(0,0, heights);
        }
    }
}
