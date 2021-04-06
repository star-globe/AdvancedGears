using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
	public class LocalPlayerController : MonoBehaviour
	{
		[SerializeField]
		BulletFireTrigger trigger;

        [SerializeField]
        PostureBoneContainer container;

        [SerializeField]
        CombinedAimTracer[] aimtracers;

        [SerializeField]
        Transform baseTarget;

		void Start ()
		{
			Assert.IsNotNull(trigger);
		}
	
		void Update ()
		{
            foreach (var tracer in aimtracers)
            {
                tracer.SetAimTarget(FowardTarget);
                tracer.Rotate(Time.deltaTime);
            }

            if (!trigger.IsAvailable)
                return;

            if (container == null)
                return;

            if (Input.GetKey(KeyCode.Mouse0))
            {
                foreach (var kvp in container.CannonDic)
                {
                    trigger.OnFire(kvp.Key, kvp.Value.GunId);
                }
			}
		}

        Vector3 FowardTarget
        {
            get
            {
                return baseTarget.position;
            }
        }
	}
}
