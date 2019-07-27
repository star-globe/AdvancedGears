using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker;
using Improbable.Worker.CInterop;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears.MonoBehaviours
{
    public class SpawnBaseUnitCommandReceiver : MonoBehaviour
    {
        [Require] private TransformInternalReader transformReader;
        [Require] private BaseUnitSpawnerCommandReceiver baseUnitSpawnerCommandReceiver;
        [Require] private BaseUnitSpawnerWriter baseUnitSpawnerWriter;
        [Require] private WorldCommandSender worldCommandSender;

        [Require] private ILogDispatcher logDispatcher;

        private void OnEnable()
        {
            baseUnitSpawnerCommandReceiver.OnSpawnUnitRequestReceived += OnSpawnUnitRequest;
        }

        private void OnSpawnUnitRequest(BaseUnitSpawner.SpawnUnit.ReceivedRequest receivedRequest)
        {
            baseUnitSpawnerCommandReceiver.SendSpawnUnitResponse(
                new BaseUnitSpawner.SpawnUnit.Response(receivedRequest.RequestId, new Empty()));

            var request = new WorldCommands.ReserveEntityIds.Request
            {
                NumberOfEntityIds = 1,
                Context = this,
            };
            worldCommandSender.SendReserveEntityIdsCommand(request, OnEntityIdsReserved);
        }

        private void OnEntityIdsReserved(WorldCommands.ReserveEntityIds.ReceivedResponse response)
        {
            if (!ReferenceEquals(this, response.Context))
            {
                // This response was not for a command from this behaviour.
                return;
            }

            if (response.StatusCode != StatusCode.Success)
            {
                logDispatcher.HandleLog(LogType.Error,
                    new LogEvent("ReserveEntityIds failed.")
                        .WithField("Reason", response.Message));
                return;
            }

            var location = transformReader.Data.Location;
            var unitEntityTemplate =
                BaseUnitTemplate.CreateBaseUnitEntityTemplate(UnitSide.A, new Coordinates(location.X, location.Y + 2, location.Z), UnitType.Soldier);
            var expectedEntityId = response.FirstEntityId.Value;

            worldCommandSender.SendCreateEntityCommand(
                new WorldCommands.CreateEntity.Request(unitEntityTemplate, expectedEntityId), OnEntityCreated);
        }

        private void OnEntityCreated(WorldCommands.CreateEntity.ReceivedResponse response)
        {
            if (!ReferenceEquals(this, response.Context))
            {
                // This response was not for a command from this behaviour.
                return;
            }

            if (response.StatusCode != StatusCode.Success)
            {
                logDispatcher.HandleLog(LogType.Error,
                    new LogEvent("CreateEntity failed.")
                        .WithField(LoggingUtils.EntityId, response.RequestPayload.EntityId)
                        .WithField("Reason", response.Message));

                return;
            }

            var spawnedUnitsCopy =
                new List<EntityId>(baseUnitSpawnerWriter.Data.SpawnedUnits);
            var newEntityId = response.EntityId.Value;

            spawnedUnitsCopy.Add(newEntityId);

            baseUnitSpawnerWriter.SendUpdate(new BaseUnitSpawner.Update
            {
                SpawnedUnits = spawnedUnitsCopy
            });
        }
    }
}

