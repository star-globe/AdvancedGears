using Improbable.Gdk.GameObjectRepresentation;
using UnityEngine;

namespace Playground
{
    [WorkerType(WorkerUtils.UnityClient)]
    public class BaseUnitClientVisibility : MonoBehaviour
    {
        [Require] private BaseUnit.Requirable.Reader baseUnitReader;//HealthPickup.Requirable.Reader healthPickupReader;

        //private MeshRenderer cubeMeshRenderer;

        private void OnEnable()
        {
            //cubeMeshRenderer = GetComponentInChildren<MeshRenderer>();
            //baseUnitReader.ComponentUpdated += OnHealthPickupComponentUpdated;
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            //cubeMeshRenderer.enabled = healthPickupReader.Data.IsActive;

        }

         private void OnHealthPickupComponentUpdated(BaseUnit.Update update)
         {
            UpdateVisibility();

            
         }
    }
}
