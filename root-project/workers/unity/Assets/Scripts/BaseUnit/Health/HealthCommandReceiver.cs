using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class HealthCommandReceiver : MonoBehaviour
    {
        [Require] World world;
        [Require] BaseUnitHealthCommandReceiver healthCommandReceiver;
        [Require] BaseUnitHealthWriter healthWriter;
        [Require] BaseUnitStatusWriter statusWriter;
        [Require] Entity entity;

        LinkedEntityComponent spatialComp = null;
        LinkedEntityComponent SpatialComp
        {
            get
            {
                if (spatialComp == null)
                    spatialComp = GetComponent<LinkedEntityComponent>();

                return spatialComp;
            }
        }

        BaseUnitReviveTimerSystem unitReviveSystem = null;
        BaseUnitReviveTimerSystem UnitReviveSystem
        {
            get
            {
                unitReviveSystem = unitReviveSystem ?? world.GetExistingSystem<BaseUnitReviveTimerSystem>();
                return unitReviveSystem;
            }
        }

        PlayerReviveTimerSystem playerReviveSystem = null;
        PlayerReviveTimerSystem PlayerReviveSystem
        {
            get
            {
                playerReviveSystem = playerReviveSystem ?? world.GetExistingSystem<PlayerReviveTimerSystem>();
                return playerReviveSystem;
            }
        }


        public void OnEnable()
        {
            healthCommandReceiver.OnModifyHealthRequestReceived += OnModifiedHealthRequest;
        }

        private void OnModifiedHealthRequest(BaseUnitHealth.ModifyHealth.ReceivedRequest request)
        {
            healthCommandReceiver.SendModifyHealthResponse(new BaseUnitHealth.ModifyHealth.Response(request.RequestId, new Empty()));

            var health = request.Payload.Amount;
            if (CheckAndUpdateHealth(health) == false)
                return;

            var status = statusWriter.Data;

            UnitState state = UnitState.None;
            if (health > 0) {
                if (status.State == UnitState.Sleep)
                    state = UnitState.Alive;
            }
            else
                state = UnitState.Dead;

            if (state == UnitState.None)
                return;

            var side = status.Side;
            if (UnitUtils.IsBuilding(status.Type))
                side = UnitSide.None;

            statusWriter.SendUpdate(new BaseUnitStatus.Update()
            {
                State = state,
                Side = side,
            });

            if (state == UnitState.Dead) {
                if (world.EntityManager.HasComponent<BaseUnitReviveTimer.Component>(entity))
                    this.UnitReviveSystem?.AddDeadUnit(SpatialComp.EntityId);
                else if (world.EntityManager.HasComponent<PlayerRespawn.Component>(entity))
                    this.PlayerReviveSystem?.AddDeadUnit(SpatialComp.EntityId);
            }
        }

        private bool CheckAndUpdateHealth(int health)
        {
            var current= healthWriter.Data.Health;

            if (current == health)
                return false;

            healthWriter.SendUpdate(new BaseUnitHealth.Update()
            {
                Health = health,
            });

            return true;
        }
    }
}
