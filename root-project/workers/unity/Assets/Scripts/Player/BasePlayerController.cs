using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
	public abstract class BasePlayerController : MonoBehaviour
	{
		[SerializeField]
		BulletFireTrigger trigger;

        [SerializeField]
        PostureBoneContainer container;

        [SerializeField]
        CombinedAimTracer[] aimtracers;

        [SerializeField]
        Transform baseTarget;

        List<Vector3> enemyPosList = null;
        Vector3? targetPosition = null;

        void Start ()
        {
            Assert.IsNotNull(trigger);
        }

        void Update ()
        {
            targetPosition = null;
            if (enemyPosList != null && enemyPosList.Count > 0) {
                float length = float.MaxValue;
                var pos = transform.position;
                foreach (var e in enemyPosList) {
                    var mag = (e - pos).sqrMagnitude;
                    if (mag > length)
                        continue;

                    targetPosition = e;
                    length = mag;
                }
            }
            else
            {
                targetPosition = FowardTarget;
            }


            foreach (var tracer in aimtracers)
            {
                tracer.SetAimTarget(FowardTarget);
                tracer.Rotate(Time.deltaTime);
            }

            if (!trigger.IsAvailable)
                return;

            if (container == null)
                return;

            foreach (var kvp in container.HolderDic)
            {
                var holder = kvp.Value;
                var cannon = holder.Cannon;
                if (cannon != null &&
                    (this.TriggerBits & ControllerUtils.GetBits(holder.KeyCode)) != 0)
                    trigger.OnFire(kvp.Key, cannon.GunId, SelfSide);
            }
        }

        Vector3? FowardTarget
        {
            get
            {
                if (baseTarget == null)
                    return null;

                return baseTarget.position;
            }
        }

        public void SetEnemyPosList(List<Vector3> posList)
        {
            this.enemyPosList = posList;
        }

        protected abstract long TriggerBits { get; }

        protected abstract UnitSide SelfSide { get; }
    }
}
