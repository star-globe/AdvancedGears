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
        CombinedAimTracer aimtracer;

		void Start ()
		{
			Assert.IsNotNull(trigger);
		}
	
		void Update ()
		{
            if (!trigger.IsAvailable)
                return;

            if (container == null)
                return;

            if (aimtracer != null)
            {
                aimtracer.SetAimTarget(FowardTarget);
            }

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
                return this.transform.forward * 100.0f + this.transform.position;
            }
        }
	}
}
