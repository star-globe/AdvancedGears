using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class CannonHolder : MonoBehaviour
    {
        [SerializeField]
        KeyCode keyCode;
        public KeyCode KeyCode => keyCode;

        CannonTransform cannon = null;
        public CannonTransform Cannon
        {
            get
            {
                if (cannon == null)
                    cannon = this.GetComponentInChildren<CannonTransform>();

                return cannon;
            }
        }
    }
}
