using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Playground
{
	public class LocalPlayerController : MonoBehaviour
	{
		[SerializeField]
		BulletFireTrigger trigger;

		void Start ()
		{
			Assert.IsNotNull(trigger);
		}
	
		void Update ()
		{
            if (!trigger.IsAvailable)
                return;

			if (Input.GetKey(KeyCode.Space))
			{
				trigger.OnFire();
			}
		}
	}
}
