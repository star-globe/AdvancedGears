using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class LocalPlayerController : BasePlayerController
    {
        protected override bool IsTrigger => Input.GetKey(KeyCode.Mouse0);
    }
}
