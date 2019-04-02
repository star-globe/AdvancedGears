using Improbable.Gdk.Core;
using Improbable.Worker.CInterop;
using UnityEngine;

namespace Playground
{
    public class ClientWorkerConnector : DefaultWorkerConnector
    {
#pragma warning disable 649
        [SerializeField] private StaticBulletReceiver level;
#pragma warning restore 649

        private GameObject levelInstance;

        private async void Start()
        {
            await Connect(WorkerUtils.UnityClient, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override string SelectDeploymentName(DeploymentList deployments)
        {
            // This could be replaced with a splash screen asking to select a deployment or some other user-defined logic.
            return deployments.Deployments[0].DeploymentName;
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            WorkerUtils.AddClientSystems(Worker.World);
            if (level == null)
            {
                return;
            }

            levelInstance = Instantiate(level.gameObject, transform.position, transform.rotation);
            var receive = levelInstance.GetComponent<StaticBulletReceiver>();
            receive.SetWorld(this.Worker.World);
        }

        public override void Dispose()
        {
            if (levelInstance != null)
            {
                Destroy(levelInstance);
            }

            base.Dispose();
        }
    }
}
