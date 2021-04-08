using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
	public class BattleCameraController : MonoBehaviour
	{
        [Require] Entity entity;
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

        void Start()
        {
            system = world.GetExistingSystem<LocalLockOnSystem>();
            battleCamera.Value = new BattleCameraInfo(range, rad, entity.entityId);
        }

        void Update()
        {
            if (playerController == null)
                return;

            posList.Clear();
            if (system != null) {
                var list = system.GetLockOnList(entity.entityId);
                if (list != null) {
                    foreach (var u in list)
                        posList.Add(u.pos);
                }
            }

            playerController.SetEnemyPosList(posList);
        }
	}
}
