using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AdvancedGears
{
    public class NavMeshTest : MonoBehaviour
    {
        

        // Start is called before the first frame update
        void Start()
        {

        }

        private void CheckPath()
        {
            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(Vector3.zero, Vector3.one, NavMesh.AllAreas, path);

            switch (path.status)
            {
                case NavMeshPathStatus.PathComplete:
                case NavMeshPathStatus.PathInvalid:
                case NavMeshPathStatus.PathPartial:
                    break;
            }
        }
    }

}
