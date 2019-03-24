using Improbable;
using Improbable.Common;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.GameObjectRepresentation;
using Improbable.Transform;
using Improbable.Worker;
using Improbable.Worker.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Playground.MonoBehaviours
{
    public class SpawnBaseUnitCommandReceiver : MonoBehaviour
    {
        [Require] private TransformInternal.Requirable.Reader transformReader;
        [Require] private BaseUnitSpawner.Requirable.CommandRequestHandler baseUnitSpawnerCommandRequestHandler;
        [Require] private BaseUnitSpawner.Requirable.Writer baseUnitSpawnerWriter;
        [Require] private WorldCommands.Requirable.WorldCommandRequestSender worldCommandRequestSender;
        [Require] private WorldCommands.Requirable.WorldCommandResponseHandler worldCommandResponseHandler;

        private ILogDispatcher logDispatcher;

        private void OnEnable()
        {
            logDispatcher = GetComponent<SpatialOSComponent>().Worker.LogDispatcher;
            baseUnitSpawnerCommandRequestHandler.OnSpawnUnitRequest += OnSpawnUnitRequest;
            worldCommandResponseHandler.OnReserveEntityIdsResponse += OnEntityIdsReserved;
            worldCommandResponseHandler.OnCreateEntityResponse += OnEntityCreated;
        }

        private void OnSpawnUnitRequest(BaseUnitSpawner.SpawnUnit.RequestResponder requestResponder)
        {
            requestResponder.SendResponse(new Empty());

            worldCommandRequestSender.ReserveEntityIds(1, context: this);
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
                BaseUnitTemplate.CreateBaseUnitEntityTemplate(1, new Coordinates(location.X, location.Y + 2, location.Z));
            var expectedEntityids = response.FirstEntityId.Value;

            worldCommandRequestSender.CreateEntity(unitEntityTemplate, expectedEntityids, context:this);
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

            baseUnitSpawnerWriter.Send(new BaseUnitSpawner.Update
            {
                SpawnedUnits = spawnedUnitsCopy
            });
        }
    }
}

