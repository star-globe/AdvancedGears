using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
	public class BattleCameraController : MonoBehaviour
	{
        //[Require] SpatialEntityId entityId;
        [Require] World world;

        [SerializeField]
        float range = 100.0f;

        [SerializeField]
        float rad = 60.0f;

        [SerializeField]
        BattleCameraComponent battleCamera;

        [SerializeField]
        LocalPlayerController playerController;

        LocalLockOnSystem system = null;

        readonly List<Vector3> posList = new List<Vector3>();
        long cameraId = -1;

        void Start()
        {
            system = world.GetExistingSystem<LocalLockOnSystem>();
            battleCamera.Value = new BattleCameraInfo(range, rad, cameraId);
        }

        void Update()
        {
            if (playerController == null)
                return;

            posList.Clear();
            if (system != null) {
                var list = system.GetLockOnList(cameraId);
                if (list != null) {
                    foreach (var u in list)
                        posList.Add(u.pos);
                }
            }

            playerController.SetEnemyPosList(posList);
        }
	}
}
