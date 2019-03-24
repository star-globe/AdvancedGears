using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Playground
{
	public class LocalPlayerController : MonoBehaviour
	{
		[SerializeField]
		ClientShootings shootings;

		void Start ()
		{
			Assert.IsNotNull(shootings);
		}
	
		void Update ()
		{
			if (Input.GetKey(KeyCode.Space))
			{
				shootings.OnFire();
			}
		}
	}
}
