using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class BaseUnitInitializer : MonoBehaviour
    {
        [Require] BaseUnitMovementWriter movement;
        [Require] BaseUnitSightWriter sight;
        [Require] BaseUnitActionWriter action;
        [Require] BaseUnitHealthWriter health;
        [Require] FuelComponentWriter fuel;

        [SerializeField]
        BaseUnitInitSettings settings;

        [SerializeField]
        GunComponentInitializer gunInitializer;

        void Start()
        {
            Assert.IsNotNull(settings);

            movement.SendUpdate(new BaseUnitMovement.Update
            {
                MoveSpeed = settings.Speed,
                RotSpeed = settings.RotSpeed,
                ConsumeRate = settings.ConsumeRate,
            });

            sight.SendUpdate(new BaseUnitSight.Update
            {
                Interval = IntervalCheckerInitializer.InitializedChecker(settings.Inter),
                Range = settings.SightRange
            });

            action.SendUpdate(new BaseUnitAction.Update
            {
                Interval = IntervalCheckerInitializer.InitializedChecker(settings.Inter),
                AngleSpeed = settings.AngleSpeed,
            });

            health.SendUpdate(new BaseUnitHealth.Update
            {
                MaxHealth = settings.MaxHp,
                Health = settings.MaxHp,
                Defense = settings.Defense,
            });

            fuel.SendUpdate(new FuelComponent.Update
            {
                MaxFuel = settings.MaxFuel,
                Fuel = settings.MaxFuel,
            });

            if (gunInitializer != null)
                gunInitializer.SetGunIds(settings.GunIds);
        }
    }
}
