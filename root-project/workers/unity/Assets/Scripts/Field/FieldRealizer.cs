using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine.AI;
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

        float[,] heights = null;
        float[,] Heights
        {
            get
            {
                if (heights == null)
                {
                    var width = terrain.terrainData.heightmapResolution;
                    var height = terrain.terrainData.heightmapResolution;
                    heights = new float[width, width];
                }

                return heights;
            }
            set
            {
                heights = value;
            }
        }


        public bool IsSet { get; private set;}

        private void Start()
        {
            Assert.IsNotNull(terrain);
            Assert.IsNotNull(collider);
        }

        public void Setup(float fieldSize, FieldDictionary dictionary = null)
        {
            var dic = dictionary ?? FieldDictionary.Instance;

            var terrainData = Instantiate(dic.BaseTerrainData);
            var height = dic.GetHeight(fieldSize);

            terrainData.heightmapResolution = dic.GetResolution(fieldSize);
            terrainData.size = new Vector3(fieldSize, height, fieldSize);

            Debug.LogFormat("size:{0} resolution:{1}", terrainData.size, terrainData.heightmapResolution);

            terrain.terrainData = terrainData;
            collider.terrainData = terrainData;

            var surface = this.GetComponent<NavMeshSurface>();
            if (surface != null)
            {
                surface.center = terrainData.bounds.center;
                surface.size = terrainData.bounds.size;
                surface.BuildNavMesh();
                Debug.Log("BuildNavMesh");
            }
        }

        public void Reset()
        {
            this.IsSet = false;
            heights = null;
        }

        public void Realize(Vector3 center, List<TerrainPointInfo> terrainPoints = null, Vector3? terrainPos = null)
        {
            this.IsSet = true;
            var width = terrain.terrainData.heightmapResolution;
            var height = terrain.terrainData.heightmapResolution;
            var size = terrain.terrainData.size;
            var start = center - new Vector3(size.x/2, 0.0f, size.z/2);

            this.transform.position = start;

            Debug.LogFormat("Start:{0}",start);

            float[,] heights = Heights;
            Vector3 pos = terrainPos != null ? terrainPos.Value: Vector3.zero;
            if (terrainPoints != null)
            {
                foreach (var point in terrainPoints)
                {
                    //Debug.LogFormat("Height:{0} TileSize:{1}", point.HighestHillHeight, point.TileSize);
                    heights = point.SetHeights(pos, start.x, start.z, width, height, size, heights);
                }
            }

            Heights = heights;
            terrain.terrainData.SetHeights(0,0, heights);
        }
    }
}
