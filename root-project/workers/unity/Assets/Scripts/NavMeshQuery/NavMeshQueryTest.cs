using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using Unity.Collections;
using System.Collections;

namespace AdvancedGears
{
    public class NavMeshQueryTest : MonoBehaviour
    {
        [SerializeField]
        Transform start;

        [SerializeField]
        Transform end;

        const int extents = 10;
        const int maxPath = 32;
        IEnumerator StartQuery()
        {
            NavMeshWorld world = NavMeshWorld.GetDefaultWorld();
            NavMeshQuery query = new NavMeshQuery(world, Allocator.Persistent, maxPath);

            NavMeshLocation startLocation = query.MapLocation(start.position, Vector3.up * extents, 0);
            NavMeshLocation endLocation = query.MapLocation(end.position, Vector3.up * extents, 0);
            PathQueryStatus status = query.BeginFindPath(startLocation, endLocation);

            yield return new WaitWhile(() => {
                status = query.UpdateFindPath(8, out int iterationsPerformed);
                return status == PathQueryStatus.InProgress;
            });

            status = query.EndFindPath(out int pathsize);

            NativeArray<PolygonId> path = new NativeArray<PolygonId>(pathsize, Allocator.Temp);
            int pathResult = query.GetPathResult(path);
            NativeArray<NavMeshLocation> pathStraight = new NativeArray<NavMeshLocation>(maxPath, Allocator.Temp);
            //NativeArray<StraightPathFlag> pathStreaigthFlag = new NativeArray<StraightPathFlags>(maxPath, Allocator.Temp);
            NativeArray<float> vertexSize = new NativeArray<float>(maxPath, Allocator.Temp);

            int straghtPathCount = 0;
            query.Dispose();
        }
    }
}
