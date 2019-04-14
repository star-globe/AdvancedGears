using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;

namespace Playground
{
    public class BaseUnitInitializer : MonoBehaviour
    {
        [Require] BaseUnitMovementWriter movement;
        [Require] BaseUnitSightWriter sight;
        [Require] BaseUnitActionWriter action;
        [Require] BaseUnitHealthWriter health;

        float speed = 1.0f;
        float rot = 1.8f;
        float inter = 3.0f;
        float sightRange = 10.0f;
        float atkRange = 9.0f;
        float atkAngle = Mathf.PI * (30.0f / 360);

        void Start()
        {
            movement.SendUpdate(new BaseUnitMovement.Update
            {
                MoveSpeed = speed,
                RotSpeed = rot
            });

            sight.SendUpdate(new BaseUnitSight.Update
            {
                Interval = inter,
                LastSearched = 0,
                Range = sightRange
            });

            action.SendUpdate(new BaseUnitAction.Update
            {
                Interval = inter,
                LastActed = 0,
                AttackRange = atkRange,
                AttackAngle = atkAngle
            });

            health.SendUpdate(new BaseUnitHealth.Update
            {
                MaxHealth = 100,
                Health = 100,
                Defense = 10,
            });
        }
    }
}
