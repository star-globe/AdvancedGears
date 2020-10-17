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

            if (Input.GetKey(KeyCode.Space))
            {
                foreach (var kvp in container.CannonDic)
                {
                    trigger.OnFire(kvp.Key, kvp.Value.GunId);
                }
			}
		}
	}
}
