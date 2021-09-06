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
        [Require] PlayerRespawnReader reader;
        [Require] PositionWriter positionWriter;

        private void OnEnable()
        {
            reader.OnRespawnEvent += OnRespawn;
        }

        private void OnRespawn(Empty empty)
        {
            positionWriter.SendUpdate(new Position.Update
            {
                Coords = reader.Data.Position,
            });
        }
    }
}
