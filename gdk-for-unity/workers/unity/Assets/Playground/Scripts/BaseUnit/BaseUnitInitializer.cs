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
        BaseUnitInitSettings settings;

        void Start()
        {
            movement.SendUpdate(new BaseUnitMovement.Update
            {
                MoveSpeed = settings.Speed,
                RotSpeed = settings.Rot * Mathf.Deg2Rad
            });

            sight.SendUpdate(new BaseUnitSight.Update
            {
                Interval = new IntervalChecker(settings.Inter,0),
                Range = settings.SightRange
            });

            action.SendUpdate(new BaseUnitAction.Update
            {
                Interval = new IntervalChecker(settings.Inter,0),
                AttackRange = settings.AtkRange,
                AttackAngle = settings.AtkAngle * Mathf.Deg2Rad,
                AngleSpeed = settings.AngleSpeed * Mathf.Deg2Rad,
            });

            health.SendUpdate(new BaseUnitHealth.Update
            {
                MaxHealth = settings.MaxHealth,
                Health = settings.MaxHealth,
                Defense = settings.Defense,
            });
        }
    }
}
