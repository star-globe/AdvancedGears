using System;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Worker.CInterop;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TransformSynchronization;
using UnityEngine;

namespace AdvancedGears
{
    public class UnityClientConnector : WorkerConnector
    {
        [SerializeField]
        UnitSide side;

        [SerializeField]
        Vector3 pos;

        public const string WorkerType = WorkerUtils.UnityClient;

        private async void Start()
        {
            var connParams = CreateConnectionParameters(WorkerType);
            connParams.Network.ConnectionType = NetworkConnectionType.Kcp;

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

            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            WorkerUtils.AddClientSystems(Worker.World, this.gameObject, false);

            var fieldSystem = Worker.World.GetExistingSystem<FieldQueryClientSystem>();
            if (fieldSystem != null)
            {
                fieldSystem.OnQueriedEvent += CreatePlayerRequest;
                fieldSystem.SetXZPosition(pos.x, pos.z);
            }
        }

        void CreatePlayerRequest()
        {
            var system = Worker.World.GetExistingSystem<SendCreatePlayerRequestSystem>();
            if (system == null)
                return;

            var origin = new Vector3(pos.x, FieldDictionary.WorldHeight, pos.z) + this.Worker.Origin;
            var point = PhysicsUtils.GetGroundPosition(origin);
            
            system.RequestPlayerCreation(SerializeUtils.SerializeArguments(new PlayerInitInfo(side, point - this.Worker.Origin)));
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
