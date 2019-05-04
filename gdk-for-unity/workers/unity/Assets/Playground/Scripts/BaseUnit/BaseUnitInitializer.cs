using System;
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

        [SerializeField]
        float speed = 1.0f;

        [SerializeField]
        float rot =0.3f;

        float inter = 0.5f;

        [SerializeField]
        float sightRange = 10.0f;

        [SerializeField]
        float atkRange = 9.0f;

        [SerializeField]
        float atkAngle = 5.0f;

        [SerializeField]
        float angleSpeed = 5.0f;

        void Start()
        {
            movement.SendUpdate(new BaseUnitMovement.Update
            {
                MoveSpeed = speed,
                RotSpeed = rot * Mathf.Deg2Rad
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
                AttackAngle = atkAngle * Mathf.Deg2Rad,
                AngleSpeed = angleSpeed * Mathf.Deg2Rad,
            });

            health.SendUpdate(new BaseUnitHealth.Update
            {
                MaxHealth = 10,
                Health = 10,
                Defense = 10,
            });
        }
    }
}
