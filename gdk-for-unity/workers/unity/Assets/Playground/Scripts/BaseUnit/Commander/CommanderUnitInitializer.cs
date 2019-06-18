using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace Playground
{
    public class CommanderUnitInitializer : MonoBehaviour
    {
        [Require] CommanderSightWriter sight;
        [Require] CommanderStatusWriter commander;
        [Require] BaseUnitStatusReader status;
        [Require] World world;

        float inter = 2.0f;

        [SerializeField]
        float sightRange = 100.0f;

        [SerializeField]
        float allyRange = 50.0f;

        void Start()
        {
            sight.SendUpdate(new CommanderSight.Update
            {
                Interval = new IntervalChecker(inter,0),
                Range = sightRange
            });

            Invoke("DelayMethod", 3.5f);
        }

        void DelayMethod()
        {
            var entityManager = world.GetExistingManager<EntityManager>();
            if (entityManager == null)
                return;

            var selfSide = status.Data.Side;
            var list = new List<EntityId>();
            //var option = new Option<List<EntityId>>(list);

            var pos = this.transform.position;
            var colls = Physics.OverlapSphere(pos, allyRange, LayerMask.GetMask("Unit"));
            for (var i = 0; i < colls.Length; i++)
            {
                var col = colls[i];
                var comp = col.GetComponent<LinkedEntityComponent>();
                if (comp == null)
                    continue;

                Entity entity;
                if (!comp.Worker.TryGetEntity(comp.EntityId, out entity))
                    continue;

                if (!entityManager.HasComponent<BaseUnitStatus.Component>(entity))
                    continue;

                var status = entityManager.GetComponentData<BaseUnitStatus.Component>(entity);
                if (status.Side == selfSide)
                    list.Add(comp.EntityId);
            }

            commander.SendUpdate(new CommanderStatus.Update
            {
                AllyRange = allyRange,
                FollowerInfo = new FollowerInfo { Followers = list.ToList(), UnderCommanders = new List<EntityId>() },
            });
        }
    }
}
