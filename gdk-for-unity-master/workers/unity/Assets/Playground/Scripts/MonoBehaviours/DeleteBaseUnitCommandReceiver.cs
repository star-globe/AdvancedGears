using Improbable.Common;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.GameObjectRepresentation;
using Improbable.Worker;
using Improbable.Worker.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Playground.MonoBehaviours
{
    public class DeleteBaseUnitCommandReceiver : MonoBehaviour
    {
        [Require] private BaseUnitSpawner.Requirable.Writer baseUnitSpawnerWriter;
        [Require] private BaseUnitSpawner.Requirable.CommandRequestHandler baseUnitSpawnerCommandRequestHandler;
        [Require] private WorldCommands.Requirable.WorldCommandRequestSender worldCommandRequestSender;
        [Require] private WorldCommands.Requirable.WorldCommandResponseHandler worldCommandResponseHandler;

        private ILogDispatcher logDispacher;

        private void OnEnable()
        {
            logDispacher = GetComponent<SpatialOSComponent>().Worker.LogDispatcher;
            baseUnitSpawnerCommandRequestHandler.OnDeleteSpawnedCubeRequest += OnDeleteSpawnedCubeRequest;
            worldCommandResponseHandler.OnDeleteEntityResponse += OnDeleteEntityResponse;
        }

        private void OnDeleteSpawnedCubeRequest(BaseUnitSpawner.DeleteSpawnedCube.RequestResponder requestResponder)
        {
            var entityId = requestResponder.Request.Payload.BaseunitEntityId;
            var spawnedUnits = baseUnitSpawnerWriter.Data.SpawnedUnits;

            if (!spawnedUnits.Contains(entityId))
            {
                requestResponder.SendResponseFailure($"Requested entity id {entityId} not found in list.");
            }
            else
            {
                requestResponder.SendResponse(new Empty());
            }

            worldCommandRequestSender.DeleteEntity(entityId, context:this);
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
                logDispacher.HandleLog(LogType.Error,
                        new LogEvent("Could not delete entity.")
                            .WithField(LoggingUtils.EntityId, entityId)
                            .WithField("Reason", response.Message));
                return;
            }

            var spawnedUnitsCopy =
                new List<EntityId>(baseUnitSpawnerWriter.Data.SpawnedUnits);

            if (!spawnedUnitsCopy.Remove(entityId))
            {
                logDispacher.HandleLog(LogType.Error,
                    new LogEvent("The entity has been unexpectedly removed from the list.")
                        .WithField(LoggingUtils.EntityId, entityId));
                return;
            }

            baseUnitSpawnerWriter.Send(new BaseUnitSpawner.Update
            {
                SpawnedUnits = spawnedUnitsCopy
            });
        }
    }
}
