using System;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.Core.Representation;
using Improbable.Worker.CInterop;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TransformSynchronization;
using UnityEngine;

namespace AdvancedGears
{
    public enum ConnectionState
    {
        NotConnected,
        Connecting,
        ConnectionEstablished,
        PlayerCreated,
    }

    public class UnityClientConnector : WorkerConnector
    {
        public static UnityClientConnector Instance { get; private set; } = null;

        public ConnectionState ConnectionState { get; private set; } = ConnectionState.NotConnected;

        [SerializeField] private EntityRepresentationMapping entityRepresentationMapping = default;

        [SerializeField]
        UnitSide side;

        public const string WorkerType = WorkerUtils.UnityClient;

        Coordinates startPoint;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Debug.LogError("Another UnityClientConnector exists.");
        }

        public async void StartConnect()
        {
            var connParams = CreateConnectionParameters(WorkerType);
            connParams.Network.ConnectionType = NetworkConnectionType.ModularKcp;

            var builder = new SpatialOSConnectionHandlerBuilder()
                .SetConnectionParameters(connParams);

            if (!Application.isEditor)
            {
                var initializer = new CommandLineConnectionFlowInitializer();
                switch (initializer.GetConnectionService())
                {
                    case ConnectionService.Receptionist:
                        builder.SetConnectionFlow(new ReceptionistFlow(CreateNewWorkerId(WorkerType), initializer));
                        break;
                    case ConnectionService.Locator:
                        builder.SetConnectionFlow(new LocatorFlow(initializer));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                builder.SetConnectionFlow(new ReceptionistFlow(CreateNewWorkerId(WorkerType)));
            }

            this.ConnectionState = ConnectionState.Connecting;

            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, new SyncTransObjectCreation(Worker), entityRepresentationMapping);
            WorkerUtils.AddClientSystems(Worker.World, false);

            this.ConnectionState = ConnectionState.ConnectionEstablished;
        }

        void SetFieldQueryClientSystem(Coordinates? point)
        {
            if (point == null)
            {
                Debug.LogErrorFormat("There is no SpawnPoint type:{0}", SpawnType.Start);
                return;
            }

            this.startPoint = point.Value;
            var pos = startPoint.ToUnityVector();
            var fieldSystem = Worker.World.GetExistingSystem<FieldQueryClientSystem>();
            if (fieldSystem != null)
            {
                fieldSystem.OnQueriedEvent += CreatePlayerRequest;
                fieldSystem.SetXZPosition(pos.x, pos.z);
            }
        }

        public void JoinFieldRequest()
        {
            var spawnPointSystem = Worker.World.GetExistingSystem<SpawnPointQuerySystem>();
            if (spawnPointSystem == null)
            {
                Debug.LogErrorFormat("There is no SpawnPointQuerySystem. {0}", this.Worker.World);
                return;
            }

            var current = this.transform.position - this.Worker.Origin;
            spawnPointSystem.RequestGetNearestSpawn(side, SpawnType.Start, current.ToCoordinates(), SetFieldQueryClientSystem);
        }

        float height = 1.0f;
        private void CreatePlayerRequest()
        {
            var system = Worker.World.GetExistingSystem<SendCreatePlayerRequestSystem>();
            if (system == null)
                return;

            var pos = startPoint.ToUnityVector();
            var point = pos + Vector3.up * height;

            Debug.Log("Send Player Create");

            system.RequestPlayerCreation(SerializeUtils.SerializeArguments(new PlayerInitInfo(side, point - this.Worker.Origin)), PlayerCreated);
        }

        private void PlayerCreated(PlayerCreator.CreatePlayer.ReceivedResponse response)
        {
            if (response.StatusCode == StatusCode.Success)
            {
                this.ConnectionState = ConnectionState.PlayerCreated;
            }
        }
    }

    [Serializable]
    public class PlayerInitInfo
    {
        public UnitSide side;
        public FixedPointVector3 pos;

        public PlayerInitInfo(UnitSide side, Vector3 pos)
        {
            this.side = side;
            this.pos = pos.ToFixedPointVector3();
        }
    }
}
