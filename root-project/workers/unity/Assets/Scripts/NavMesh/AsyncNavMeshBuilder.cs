using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AdvancedGears
{
    public class AsyncNavMeshBuilder : MonoBehaviour
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

        IEnumerator AsyncBake()
        {
            if (operation == null || surface == null)
                yield break;

            while (!operation.isDone)
                yield return null;

            surface.RemoveData();
            surface.navMeshData = data;
            if (surface.isActiveAndEnabled)
                surface.AddData();

            operation = null;
        }

        Coroutine coroutine = null;
        public void StartBake()
        {
            if (coroutine != null)
                StopCoroutine(coroutine);

            coroutine = StartCoroutine(AsyncBake());
        }

        public void SetData(Vector3 center, Vector3 size)
        {
            if (surface != null)
            {
                surface.center = center;
                surface.size = size;
            }
        }

        private void OnDestroy()
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
    }

}
