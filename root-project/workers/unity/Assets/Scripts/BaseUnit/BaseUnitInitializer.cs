using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class BaseUnitInitializer : MonoBehaviour
    {
        [Require] BaseUnitActionWriter action;
        [Require] BaseUnitHealthWriter health;
        [Require] FuelComponentWriter fuel;

        [SerializeField]
        BaseUnitInitSettings settings;

        [SerializeField]
        GunComponentInitializer gunInitializer;

        [SerializeField]
        PostureBoneContainer container;

        void Start()
        {
            Assert.IsNotNull(settings);

            action.SendUpdate(new BaseUnitAction.Update
            {
                SightRange = settings.SightRange,
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

            if (gunInitializer != null && container != null)
            {
                GunIdPair[] gunIds = null;
                if (container.CannonDic.Count > 0)
                {
                    gunIds = container.CannonDic.Select(kvp => new GunIdPair() { Id = kvp.Value.GunId, bone = kvp.Key}).ToArray();
                }

                gunInitializer.SetGunIds(gunIds);
            }
        }
    }
}
