using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

namespace AdvancedGears
{
    public class AsyncBuildNavMeshTest : TestCodeBase
    {
        [SerializeField]
        NavMeshSurface surface;

        NavMeshData data;
        AsyncOperation operation = null;

        NavMeshData InitializeBakeData(NavMeshSurface surface)
        {
            var emptySources = new List<NavMeshBuildSource>();
            var emptyBounds = new Bounds();
            return UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(surface.GetBuildSettings(), emptySources, emptyBounds
                , surface.transform.position, surface.transform.rotation);
        }

        private void Update()
        {
            if (operation == null)
                return;

            if (operation.isDone)
            {
                surface.RemoveData();
                surface.navMeshData = data;
                if (surface.isActiveAndEnabled)
                    surface.AddData();
                SceneView.RepaintAll();
                operation = null;
            }
        }


        [EditorButton]
        private void AsyncBuildStart()
        {
            data = InitializeBakeData(surface);
            operation = surface.UpdateNavMesh(data);
        }
    }

}
