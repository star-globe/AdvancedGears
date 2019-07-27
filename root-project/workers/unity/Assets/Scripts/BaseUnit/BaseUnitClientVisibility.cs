using Improbable.Gdk.Subscriptions;
using UnityEngine;

namespace AdvancedGears
{
    [WorkerType(WorkerUtils.UnityClient)]
    public class BaseUnitClientVisibility : MonoBehaviour
    {
        //[Require] private BaseUnitReader baseUnitReader;//HealthPickup.Requirable.Reader healthPickupReader;

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

         //private void OnHealthPickupComponentUpdated(BaseUnit.Update update)
         //{
         //   UpdateVisibility();
         //
         //   
         //}
    }
}
