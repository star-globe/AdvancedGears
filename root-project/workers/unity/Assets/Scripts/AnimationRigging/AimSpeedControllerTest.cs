using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace AdvancedGears
{
    public class AimSpeedControllerTest : AimSpeedController
    {
        private void FixedUpdate()
        {
            Rotate(Time.deltaTime);
        }
    }
}
