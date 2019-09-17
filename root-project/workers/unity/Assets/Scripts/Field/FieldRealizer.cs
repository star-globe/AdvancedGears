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

        [SerializeField]
        TerrainCollider collider;

        public bool IsSet { get; private set;}

        private void Start()
        {
            Assert.IsNotNull(terrain);
            Assert.IsNotNull(collider);
        }

        public void Setup(float fieldSize)
        {
            var terrainData = new TerrainData();
            var height = FieldDictionary.Instance.FieldHeight;

            terrainData.size = new Vector3(fieldSize, height, fieldSize);
            terrainData.heightmapResolution = FieldDictionary.GetResolution(fieldSize);

            terrain.terrainData = terrainData;
            collider.terrainData = terrainData;
        }

        public void Reset()
        {
            this.IsSet = false;
        }

        public void Realize(Vector3 center, List<TerrainPointInfo> terrainPoints = null, Vector3? terrainPos = null)
        {
            this.IsSet = true;
            var width = terrain.terrainData.heightmapWidth;
            var height = terrain.terrainData.heightmapHeight;
            var size = terrain.terrainData.size;
            var start = center - new Vector3(size.x/2, 0.0f, size.z/2);

            this.transform.position = start;

            float[,] heights = new float[width, width];
            Vector3 pos = terrainPoints != null ? terrainPos.Value: Vector3.zero;
            if (terrainPoints != null) {
                foreach (var point in terrainPoints)
                    heights = point.SetHeights(pos, start.x, start.z, width, height, size, heights);
            }

            terrain.terrainData.SetHeights(0,0, heights);
        }
    }
}
