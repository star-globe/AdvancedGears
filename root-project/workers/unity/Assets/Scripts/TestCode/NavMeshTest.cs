using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

namespace AdvancedGears
{
    public class NavMeshTest : TestCodeBase
    {
        [SerializeField]
        Transform start;

        [SerializeField]
        Transform end;

        [SerializeField]
        GameObject markerObject;

        [SerializeField]
        List<GameObject> markerList = new List<GameObject>();

        Vector3[] points = new Vector3[256];

        [EditorButton]
        public void CheckPath()
        {
            if (start == null || end == null)
                return;

            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path);

            PathClear();

            switch (path.status)
            {
                case NavMeshPathStatus.PathComplete:
                case NavMeshPathStatus.PathPartial:
                    var count = path.GetCornersNonAlloc(points);
                    SetPaths(count, points);
                    break;
                case NavMeshPathStatus.PathInvalid:
                    Debug.Log("PathInvalid");
                    break;
            }
        }

        void PathClear()
        {
            foreach (var m in markerList)
            {
                DestroyImmediate(m);
            }

            markerList.Clear();
        }

        void SetPaths(int count, Vector3[] points)
        {
            for (var i = 0; i < count; i++)
            {
                var g = Instantiate(markerObject);
                g.SetActive(true);
                g.transform.position = points[i];
                markerList.Add(g);
            }
        }
    }

}
