using Improbable.Common;
using Improbable.Gdk.Core;
using Improbable.Worker.CInterop;
using Improbable.Gdk.Subscriptions;
using Improbable.Worker;
using Improbable.Worker.Core;
using UnityEngine;

namespace Playground.MonoBehaviours
{
    public class BaseUnitSpawnerInputBehaviour : MonoBehaviour
    {
        [Require] private PlayerInputWriter playerInputWriter;
        [Require] private BaseUnitSpawnerReader baseUnitSpawnerReader;
        [Require] private BaseUnitSpawnerCommandSender baseUnitSpawnerCommandSender;
    
        [Require] private EntityId entityId;
        [Require] private World world;

        [Require] private ILogDispatcher logDispatcher;
    
        private void OnSpawnUnitResponse(BaseUnitSpawner.SpawnUnit.ReceivedResponse response)
        {
            if (response.StatusCode != StatusCode.Success)
            {
                logDispatcher.HandleLog(LogType.Error, new LogEvent($"Spawn error: {response.Message}"));
                throw new Exception("Test Exception");
            }
        }

        private void OnDeleteSpawnedCubeResponse(BaseUnitSpawner.DeleteSpawnedCube.ReceivedResponse response)
        {
            if (response.StatusCode != StatusCode.Success)
            {
                logDispatcher.HandleLog(LogType.Error, new LogEvent($"Delete error: {response.Message}"));
                throw new Exception("Test Exception");
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
            var request = new BaseUnitSpawner.SpawnBaseUnit.Request(entityId, new Empty());
            baseUnitSpawnerCommandSender.SendSpawnUnitCommanf(request);
        }

        private void SendDeleteCommand()
        {
            var spawnedUnits = baseUnitSpawnerReader.Data.SpawnedUnits;
            if (spawnedUnits.Count == 0)
            {
                logDispatcher.HandleLog(LogType.Log, new LogEvent("No units left to delete."));
                return;
            }

            var request = new BaseUnitSpawner.DeleteSpawnedBaseUnit.Request(entityId, new DeleteBaseUnitRequest()
            {
                BaseUnitEntityId = spawnedUnits[0]
            });
            baseUnitSpawnerCommandSender.SendDeleteSpawnedCubeCommand(request, OnDeleteSpawnedCubeResponse);
        }
    }
}
