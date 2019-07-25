using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.Subscriptions;
using Improbable.Worker;
using Improbable.Worker.CInterop;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears.MonoBehaviours
{
    public class DeleteBaseUnitCommandReceiver : MonoBehaviour
    {
        [Require] private BaseUnitSpawnerWriter baseUnitSpawnerWriter;
        [Require] private BaseUnitSpawnerCommandReceiver baseUnitSpawnerReceiver;
        [Require] private WorldCommandSender worldCommandSender;

        [Require] private ILogDispatcher logDispatcher;

        private void OnEnable()
        {
            baseUnitSpawnerReceiver.OnDeleteSpawnedCubeRequestReceived += OnDeleteSpawnedCubeRequest;
        }

        private void OnDeleteSpawnedCubeRequest(BaseUnitSpawner.DeleteSpawnedCube.ReceivedRequest receivedRequest)
        {
            var entityId = receivedRequest.Payload.BaseunitEntityId;
            var spawnedUnits = baseUnitSpawnerWriter.Data.SpawnedUnits;

            if (!spawnedUnits.Contains(entityId))
            {
                baseUnitSpawnerReceiver.SendDeleteSpawnedCubeResponse(
                       new BaseUnitSpawner.DeleteSpawnedCube.Response(receivedRequest.RequestId,
                           $"Requested entity id {entityId} not found in list."));
            }
            else
            {
                baseUnitSpawnerReceiver.SendDeleteSpawnedCubeResponse(
                    new BaseUnitSpawner.DeleteSpawnedCube.Response(receivedRequest.RequestId, new Empty()));
            }

            worldCommandSender.SendDeleteEntityCommand(new WorldCommands.DeleteEntity.Request(entityId),
                OnDeleteEntityResponse);
        }

        private void OnDeleteEntityResponse(WorldCommands.DeleteEntity.ReceivedResponse response)
        {
            if (!ReferenceEquals(this, response.Context))
            {
                // This response was not for a command from this behaviour.
                return;
            }

            var entityId = response.RequestPayload.EntityId;

            if (response.StatusCode != StatusCode.Success)
            {
                logDispatcher.HandleLog(LogType.Error,
                        new LogEvent("Could not delete entity.")
                            .WithField(LoggingUtils.EntityId, entityId)
                            .WithField("Reason", response.Message));
                return;
            }

            var spawnedUnitsCopy =
                new List<EntityId>(baseUnitSpawnerWriter.Data.SpawnedUnits);

            if (!spawnedUnitsCopy.Remove(entityId))
            {
                logDispatcher.HandleLog(LogType.Error,
                    new LogEvent("The entity has been unexpectedly removed from the list.")
                        .WithField(LoggingUtils.EntityId, entityId));
                return;
            }

            baseUnitSpawnerWriter.SendUpdate(new BaseUnitSpawner.Update
            {
                SpawnedUnits = spawnedUnitsCopy
            });
        }
    }
}
