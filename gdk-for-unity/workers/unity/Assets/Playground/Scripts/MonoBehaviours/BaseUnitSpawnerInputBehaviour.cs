using Improbable.Common;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectRepresentation;
using Improbable.Worker;
using Improbable.Worker.Core;
using UnityEngine;

namespace Playground.MonoBehaviours
{
    public class BaseUnitSpawnerInputBehaviour : MonoBehaviour
    {
        [Require] private PlayerInput.Requirable.Writer playerInputWriter;
        [Require] private BaseUnitSpawner.Requirable.Reader baseUnitSpawnerReader;
        [Require] private BaseUnitSpawner.Requirable.CommandRequestSender baseUnitSpawnerCommandSender;
        [Require] private BaseUnitSpawner.Requirable.CommandResponseHandler baseUnitSpawnerResponseHandler;

        private ILogDispatcher logDispatcher;
        private EntityId ownEntityId;

        private void OnEnable()
        {
            var spatialOSComponent = GetComponent<SpatialOSComponent>();
            logDispatcher = spatialOSComponent.Worker.LogDispatcher;
            ownEntityId = spatialOSComponent.SpatialEntityId;

            baseUnitSpawnerResponseHandler.OnSpawnUnitResponse += OnSpawnUnitResponse;
            baseUnitSpawnerResponseHandler.OnDeleteSpawnedCubeResponse += OnDeleteSpawnedCubeResponse;
        }

        private void OnSpawnUnitResponse(BaseUnitSpawner.SpawnUnit.ReceivedResponse response)
        {
            if (response.StatusCode != StatusCode.Success)
            {
                logDispatcher.HandleLog(LogType.Error, new LogEvent($"Spawn error: {response.Message}"));
            }
        }

        private void OnDeleteSpawnedCubeResponse(BaseUnitSpawner.DeleteSpawnedCube.ReceivedResponse response)
        {
            if (response.StatusCode != StatusCode.Success)
            {
                logDispatcher.HandleLog(LogType.Error, new LogEvent($"Delete error: {response.Message}"));
            }
        }

        private void Update()
        {
            if (playerInputWriter.Authority != Authority.Authoritative)
            {
                // Only send commands if we're the player with input.
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                SendSpawnCommand();
            }

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                SendDeleteCommand();
            }
        }

        private void SendSpawnCommand()
        {
            baseUnitSpawnerCommandSender.SendSpawnUnitRequest(ownEntityId, new Empty());
        }

        private void SendDeleteCommand()
        {
            var spawnedUnits = baseUnitSpawnerReader.Data.SpawnedUnits;
            if (spawnedUnits.Count == 0)
            {
                logDispatcher.HandleLog(LogType.Log, new LogEvent("No units left to delete."));
                return;
            }

            baseUnitSpawnerCommandSender.SendDeleteSpawnedCubeRequest(ownEntityId, new DeleteBaseUnitRequest
            {
                BaseunitEntityId = spawnedUnits[0]
            });
        }
    }
}
