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

        public static readonly float FieldSize = 1000.0f;
        public bool IsSet { get; private set;}

        private void Start()
        {
            Assert.IsNotNull(terrain);
            var size = terrain.terrainData.size;
            terrain.terrainData.size = new Vector3(FieldSize, size.y, FieldSize);
        }

        public void Reset()
        {
            this.IsSet = false;
        }

        public void Realize(Vector3 center, List<TerrainPointInfo> terrainPoints = null, Vector3 terrainPos = Vector3.zero)
        {
            this.IsSet = true;
            var width = terrain.terrainData.heightmapWidth;
            var height = terrain.terrainData.heightmapHeight;
            var size = terrain.terrainData.size;
            var start = center - new Vector3(size.x/2, 0.0f, size.z/2);

            this.transform.position = start;

            float[,] heights = new float[width, width];

            if (terrainPoints != null) {
                foreach (var point in terrainPoints)
                    heights = point.SetHeights(terrainPos, start.x, start.z, width, height, size, heights);
            }

            terrain.terrainData.SetHeights(0,0, heights);
        }
    }
}
