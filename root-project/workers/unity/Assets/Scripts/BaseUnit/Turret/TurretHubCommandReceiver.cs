using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class TurretHubCommandReceiver : MonoBehaviour
    {
        [Require] World world;
        [Require] TurretHubReader turretHub;
        [Require] BaseUnitStatusReader status;

        ComponentUpdateSystem updateSystem = null;
        ComponentUpdateSystem UpdateSystem
        {
            get
            {
                updateSystem = updateSystem ?? world.GetExistingSystem<ComponentUpdateSystem>();
                return updateSystem;
            }
        }

        public void OnEnable()
        {
            status.OnForceStateEvent += OnForceState;
        }

        private void OnForceState(ForceStateChange forceState)
        {
            var datas = turretHub.Data.TurretsDatas;

            foreach (var kvp in datas) {
                UpdateSystem.SendEvent(new BaseUnitStatus.ForceState.Event(forceState), kvp.Value.EntityId);
            }
        }
    }
}
