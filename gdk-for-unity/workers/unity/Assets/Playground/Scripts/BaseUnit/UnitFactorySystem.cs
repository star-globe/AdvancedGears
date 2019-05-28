using System;
using System.Collections;
using System.Collections.Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.ReactiveComponents;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Standardtypes;
using Improbable.Worker.CInterop;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace Playground
{
    [UpdateBefore(typeof(FixedUpdate.PhysicsFixedUpdate))]
    public class UnitFactorySystem : ComponentSystem
    {
        ComponentGroup group;
        CommandSystem commandSystem;
        ILogDispatcher logDispatcher;

        private Vector3 origin;

        private class ProductOrderCotext
        {
            public ProductOrder order;
        }


        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            origin = World.GetExistingManager<WorkerSystem>().Origin;

            commandSystem = World.GetExistingManager<CommandSystem>();
            group = GetComponentGroup(
                ComponentType.Create<UnitFactory.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        protected override void OnUpdate()
        {
            HandleProductUnit();
            HandleProductResponse();
        }

        void HandleProductUnit()
        {
            var factoryData = group.GetComponentDataArray<UnitFactory.Component>();
            var statusData = group.GetComponentDataArray<BaseUnitStatus.Component>();
            var transData = group.GetComponentArray<Transform>();
            var entityIdData = group.GetComponentDataArray<SpatialEntityId>();

            for (var i = 0; i < factoryData.Length; i++)
            {
                var factory = factoryData[i];
                var status = statusData[i];
                var pos = transData[i].position;
                var entityId = entityIdData[i];

                if (status.State != UnitState.Alive)
                    continue;

                if (status.Type != UnitType.Commander)
                    continue;

                if (status.Order == OrderType.Idle)
                    continue;

                if (factory.CurrentOrder.Type == UnitType.None && factory.Orders.Count == 0)
                    continue;

                var time = Time.realtimeSinceStartup;
                var inter = factory.Interval;
                if (inter.CheckTime(time) == false)
                    continue;

                factory.Interval = inter;

                // TODO:getFromSettings;
                float timeCost = 5;


                if (factory.CurrentOrder.Type == UnitType.None)
                {
                    factory.CurrentOrder = factory.Orders[0];
                    factory.Orders.RemoveAt(0);
                    factory.ProductInterval = new IntervalChecker(timeCost, time + timeCost);
                }

                inter = factory.ProductInterval;
                if (inter.CheckTime(time))
                {
                    var current = factory.CurrentOrder;

                    current.Number--;
                    if (current.Number <= 0)
                        current.Type = UnitType.None;

                    // create unit
                    var unitEntityTemplate =
                        BaseUnitTemplate.CreateBaseUnitEntityTemplate(current.Side, new Coordinates(pos.x, pos.y, pos.z), current.Type);
                    var request = new WorldCommands.CreateEntity.Request
                    (
                        unitEntityTemplate,
                        context: new ProductOrderCotext() { order = current }
                    );
                   commandSystem.SendCommand(request);
                }

                factoryData[i] = factory;
            }
        }

        void HandleProductResponse()
        {
            var responses = commandSystem.GetResponses<WorldCommands.CreateEntity.ReceivedResponse>();
            for (var i = 0; i < responses.Count; i++)
            {
                ref readonly var response = ref responses[i];
                if (!(response.Context is ProductOrderCotext requestContext))
                {
                    // Ignore non-player entity creation requests
                    continue;
                }

                if (response.StatusCode != StatusCode.Success)
                {
                    //var responseFailed = new PlayerCreator.CreatePlayer.Response(
                    //    requestContext.createPlayerRequest.RequestId,
                    //    $"Failed to create player: \"{response.Message}\""
                    //);
                    //commandSystem.SendResponse(responseFailed);
                }
                else
                {
                    //var responseSuccess = new PlayerCreator.CreatePlayer.Response(
                    //    requestContext.createPlayerRequest.RequestId,
                    //    new CreatePlayerResponse(response.EntityId.Value)
                    //);
                    //commandSystem.SendResponse(responseSuccess);

                    // TODO:SetFollowers
                }
            }
        }
    }
}
