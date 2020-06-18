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
        [Require] TurretHubReder turretHub;
        [Require] BaseUnitStatusReader status;

        UpdateSystem updateSystem = null;
        UpdateSystem UpdateSystem
        {
            get
            {
                updateSystem = updateSystem ?? world.GetExistingSystem<UpdateSystem>();
                return updateSystem;
            }
        }

        public void OnEnable()
        {
            status.OnForceStateRequestReceived += OnSetFrontlineRequest;
        }

        private void OnForceStateRequest(BaseUnitStatus.ForceState forceState)
        {
            var datas = turretHub.Data.TurresDatas;

            foreach (var kvp in datas) {
                UpdateSystem.SendEvent(new BaseUnitStatus.ForceState.Event(forState), kvp.Value.EntityId);
            }
        }
    }
}
