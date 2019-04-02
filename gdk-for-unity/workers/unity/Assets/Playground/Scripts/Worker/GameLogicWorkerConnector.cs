using Improbable.Gdk.Core;
using UnityEngine;

namespace Playground
{
    public class GameLogicWorkerConnector : DefaultWorkerConnector
    {
#pragma warning disable 649
        [SerializeField] private StaticBulletReceiver level;
#pragma warning restore 649

        private GameObject levelInstance;

        private async void Start()
        {
            await Connect(WorkerUtils.UnityGameLogic, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            WorkerUtils.AddGameLogicSystems(Worker.World);
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
