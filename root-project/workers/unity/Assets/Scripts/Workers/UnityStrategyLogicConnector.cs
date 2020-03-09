using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker.CInterop;
using UnityEngine;

namespace AdvancedGears
{
    public class UnityStrategyLogicConnector : WorkerConnector
    {
        public const string WorkerType = WorkerUtils.UnityStrategyLogic;

        private async void Start()
        {
            IConnectionFlow flow;
            ConnectionParameters connectionParameters;

            if (Application.isEditor)
            {
                flow = new ReceptionistFlow(CreateNewWorkerId(WorkerType));
                connectionParameters = CreateConnectionParameters(WorkerType);
            }
            else
            {
                flow = new ReceptionistFlow(CreateNewWorkerId(WorkerType),
                    new CommandLineConnectionFlowInitializer());
                connectionParameters = CreateConnectionParameters(WorkerType,
                    new CommandLineConnectionParameterInitializer());
            }

            var builder = new SpatialOSConnectionHandlerBuilder()
                .SetConnectionFlow(flow)
                .SetConnectionParameters(connectionParameters);

            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            WorkerUtils.AddStrategyLogicSystems(Worker.World, this.gameObject);
        }
    }
}
