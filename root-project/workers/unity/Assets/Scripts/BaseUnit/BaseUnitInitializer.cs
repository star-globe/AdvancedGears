using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Entities;
using Improbable.Gdk;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class BaseUnitInitializer : SpatialMonoBehaviour
    {
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

            float sightRange = settings.SightRange;
            float attackRange = 0;

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
                attackRange = gunInitializer.AttackRange;
            }

            if (this.EntityManager != null &&
                this.Worker != null && this.Worker.TryGetEntity(SpatialComp.EntityId, out Entity entity)) {
                this.EntityManager.AddComponentData<UnitActionData>(entity, UnitActionData.CreateData(sightRange, attackRange));
            }
        }
    }
}
