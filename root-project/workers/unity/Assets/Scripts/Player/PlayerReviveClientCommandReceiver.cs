using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class PlayerReviveClientCommandReceiver : MonoBehaviour
    {
        [Require] World world;
        [Require] PlayerRespawnReader reader;
        [Require] PositionWriter positionWriter;

        private void OnEnable()
        {
            reader.OnRespawnEvent += OnRespawn;
        }

        private void OnRespawn(Empty empty)
        {
            var coords = reader.Data.Position;
            positionWriter.SendUpdate(new Position.Update
            {
                Coords = coords,
            });

            var worker = world.GetExistingSystem<WorkerSystem>();

            Vector3 origin = Vector3.zero;
            if (worker != null)
                origin = worker.Origin;

            this.transform.position = coords.ToWorkerPosition(origin);
        }
    }
}
