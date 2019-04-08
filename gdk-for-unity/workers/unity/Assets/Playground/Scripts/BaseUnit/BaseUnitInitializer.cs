using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;

namespace Playground
{
    public class BaseUnitInitializer : MonoBehaviour
    {
        [Require] BaseUnitMovementWriter writer;
        [Require] BaseUnitSightWriter sight;

        float speed = 1.0f;
        float rot = 1.8f;
        float inter = 3.0f;
        float ran = 10.0f;

        void Start()
        {
            writer.SendUpdate(new BaseUnitMovement.Update
            {
                MoveSpeed = speed,
                RotSpeed = rot
            });

            sight.SendUpdate(new BaseUnitSight.Update
            {
                Interval = inter,
                LastSearched = 0,
                Range = ran
            });
        }
    }
}
