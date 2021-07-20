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

        int width = 0;

        float[,] heights = null;
        float[,] Heights
        {
            get
            {
                if (heights == null)
                {
                    this.width = terrain.terrainData.heightmapResolution;
                    heights = new float[width, width];
                }

                return heights;
            }
        }

        public int X { get; private set; } = int.MaxValue;
        public int Y { get; private set; } = int.MaxValue;

        public bool IsSet { get; private set;}
        public Vector3 Center { get; private set; }

        private void Start()
        {
            Assert.IsNotNull(terrain);
            Assert.IsNotNull(collider);
        }

        public void Setup(float fieldSize, FieldDictionary dictionary = null)
        {
            var dic = dictionary ?? FieldDictionary.Instance;

            var terrainData = terrain.terrainData ?? Instantiate(dic.BaseTerrainData);
            var height = dic.GetHeight(fieldSize);

            terrainData.heightmapResolution = dic.GetResolution(fieldSize);
            terrainData.size = new Vector3(fieldSize, height, fieldSize);

            //Debug.LogFormat("size:{0} resolution:{1}", terrainData.size, terrainData.heightmapResolution);

            terrain.terrainData = terrainData;
            collider.terrainData = terrainData;

            var builder = this.GetComponent<AsyncNavMeshBuilder>();
            if (builder != null)
            {
                builder.SetData(terrainData.bounds.center, terrainData.bounds.size);
                builder.StartBake();
                //Debug.Log("BuildNavMesh");
            }
        }

        public bool SetAndCheckXY(int x, int y)
        {
            this.IsSet = true;

            if (this.X == x && this.Y == y)
                return false;

            this.X = x;
            this.Y = y;
            return true;
        }

        public void ResetField()
        {
            this.IsSet = false;
            heights = null;
        }

        public void SetCenter(Vector3 center)
        {
            this.Center = center;
        }

        public void Realize(Vector3? center = null, List<TerrainPointInfo> terrainPoints = null, Vector3? terrainPos = null)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Realize!");

            if (center != null)
                this.Center = center.Value;

            var width = terrain.terrainData.heightmapResolution;
            var height = terrain.terrainData.heightmapResolution;
            var size = terrain.terrainData.size;
            var start = this.Center - new Vector3(size.x/2, 0.0f, size.z/2);

            this.transform.position = start;

            //Debug.LogFormat("Start:{0}",start);

            ClearHeights();

            UnityEngine.Profiling.Profiler.BeginSample("SetHeights!");
            float[,] heights = Heights;
            Vector3 pos = terrainPos != null ? terrainPos.Value: Vector3.zero;
            if (terrainPoints != null)
            {
                foreach (var point in terrainPoints)
                {
                    //Debug.LogFormat("Height:{0} TileSize:{1}", point.HighestHillHeight, point.TileSize);
                    //point.SetHeights(pos, start.x, start.z, width, height, size, heights);
                }
            }

            terrain.terrainData.SetHeights(0,0, heights);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private void ClearHeights()
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    this.Heights[i, j] = 0.0f;
                }
            }
        }
    }
}
