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

        public void Realize(List<TerrainPointInfo> terrainPoints, Vector3 terrainPos, Vector3 center)
        {
            var width = terrain.terrainData.heightmapWidth;
            var height = terrain.terrainData.heightmapHeight;
            var size = terrain.terrainData.size;
            var start = center - new Vector3(size.x/2, 0.0f, size.z/2);

            this.transform.position = start;

            float[,] heights = new float[width, width];

            foreach (var point in terrainPoints)
                heights = point.SetHeights(terrainPos, start.x, start.z, width, height, size, heights);

            terrain.terrainData.SetHeights(0,0, heights);
        }
    }
}
