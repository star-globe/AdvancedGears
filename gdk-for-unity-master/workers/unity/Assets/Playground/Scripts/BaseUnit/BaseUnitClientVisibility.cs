using Improbable.Gdk.GameObjectRepresentation;
using UnityEngine;

namespace Playground
{
    [WorkerType(WorkerUtils.UnityClient)]
    public class BaseUnitClientVisibility : MonoBehaviour
    {
        [Require] private BaseUnitMoveVelocity.Requirable.Reader moveVelocityReader;//HealthPickup.Requirable.Reader healthPickupReader;

        //private MeshRenderer cubeMeshRenderer;

        private void OnEnable()
        {
            //cubeMeshRenderer = GetComponentInChildren<MeshRenderer>();
            moveVelocityReader.ComponentUpdated += OnHealthPickupComponentUpdated;
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            //cubeMeshRenderer.enabled = healthPickupReader.Data.IsActive;

        }

         private void OnHealthPickupComponentUpdated(BaseUnitMoveVelocity.Update update)
         {
            UpdateVisibility();

            
         }
    }
}
