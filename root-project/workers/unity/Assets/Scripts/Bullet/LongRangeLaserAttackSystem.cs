using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class LongRangeLaserAttackSystem : SpatialComponentSystem
    {
        class LaserContainer
        {
            public long shooterId { get; private set;}
            readonly Dictionary<ulong,LaserFireInfo> fireDic = new Dictionary<ulong, LaserFireInfo>();

            public LaserContainer(long shooterId)
            {
                this.shooterId = shooterId;
            }

            public void AddLaser(LaserFireInfo fire)
            {
                var id = fire.LaserId;
                fireDic[id] = fire;
            }
        }

        readonly Dictionary<long,LaserContainer> laserContainer = new Dictionary<long, LaserContainer>();

        protected override void OnCreate()
        {
        }

        protected override void OnUpdate()
        {
            HandleLaserFireInfoEvents();
            CreateLasers();
        }

        private void HandleLaserFireInfoEvents()
        {
            var laserFireEvents = UpdateSystem.GetEventsReceived<LongRangeLaserComponent.LaserFired.Event>();
            for (var i = 0; i < laserFireEvents.Count; i++)
            {
                var laser = laserFireEvents[i];
                var id = laser.EntityId.Id;
                if (laserContainer.ContainsKey(id) == false)
                    laserContainer.Add(id, new LaserContainer(id));

                var container = laserContainer[id];
                container.AddLaser(laser.Event.Payload);
            }
        }

        private void CreateLasers()
        {
            foreach (var kvp in laserContainer)
            {

            }
        }
    }
}
