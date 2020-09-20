using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.Core.Representation;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker.CInterop;
using UnityEngine;

namespace AdvancedGears
{
    public class UnityGameLogicConnector : WorkerConnector
    {
        [SerializeField] private EntityRepresentationMapping entityRepresentationMapping = default;

        public const string WorkerType = WorkerUtils.UnityGameLogic;

        private async void Start()
        {
            PlayerLifecycleConfig.CreatePlayerEntityTemplate = CreatePlayerEntityTemplate;
            PlayerLifecycleConfig.AutoRequestPlayerCreation = false;

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
            WorkerUtils.AddGameLogicSystems(Worker.World, entityRepresentationMapping);
        }

        private static EntityTemplate CreatePlayerEntityTemplate(EntityId entityId, string workerId, byte[] serializedArguments)
        {
            var clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);

            var initInfo = SerializeUtils.DeserializeArguments<PlayerInitInfo>(serializedArguments);
            return BaseUnitTemplate.CreateAdvancedUnitEntityTemplate(workerId, initInfo.pos.ToCoordinates(), initInfo.side);
            //var template = new EntityTemplate();
            //template.AddComponent(new Position.Snapshot { Coords = initInfo.pos.ToCoordinates() }, clientAttribute);
            //template.AddComponent(new Metadata.Snapshot("Player"), serverAttribute);
            //template.AddComponent(new BulletComponent.Snapshot(), clientAttribute);
            //template.AddComponent(new PlayerInput.Snapshot(), clientAttribute);
            //template.AddComponent(new BaseUnitStatus.Snapshot { Side = initInfo.side }, serverAttribute);

            //TransformSynchronizationHelper.AddTransformSynchronizationComponents(template, clientAttribute);
            //PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, serverAttribute);

            //template.SetReadAccess(UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, serverAttribute);
            //template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);

            //return template;
        }
    }
}
