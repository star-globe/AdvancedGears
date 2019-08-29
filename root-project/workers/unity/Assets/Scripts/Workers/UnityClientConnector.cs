using System;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Worker.CInterop;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.Subscriptions;
using UnityEngine;

namespace AdvancedGears
{
    public class UnityClientConnector : WorkerConnector
    {
        [SerializeField]
        PlayerInitInfo playerInitInfo;

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
            WorkerUtils.AddClientSystems(Worker.World, this.gameObject);

            var system = Worker.World.GetExistingSystem<SendCreatePlayerRequestSystem>();
            if (system != null)
            {
                system.RequestPlayerCreation(SerializeUtils.SerializeArguments(playerInitInfo));
            }
        }
    }

    [Serializable]
    public class PlayerInitInfo
    {
        public UnitSide side;
        public Vector3 pos;
    }
}
