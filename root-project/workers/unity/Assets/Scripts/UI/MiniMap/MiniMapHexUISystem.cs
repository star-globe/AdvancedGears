using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using AdvancedGears;

namespace AdvancedGears.UI
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class MiniMapHexUISystem : BaseMiniMapUISystem<HexUIObject>
    {
        //MiniMapUIDisplay MiniMapUIDisplay => MiniMapUIDisplay.Instance;

        private EntityQuery hexGroup;
        private EntityQueryBuilder.F_EDDDD<HexBase.Component, HexPower.Component, Position.Component, SpatialEntityId> hexAction;

        protected override UIType uiType => UIType.MiniMapHex;
        //protected override Transform parent
        //{
        //    get { return this.MiniMapUIDisplay?.MiniMapParent; }
        //}

        protected override void OnCreate()
        {
            base.OnCreate();

            hexGroup = GetEntityQuery(
                ComponentType.ReadOnly<HexBase.Component>(),
                ComponentType.ReadOnly<HexPower.Component>(),
                ComponentType.ReadOnly<Position.Component>()
            );

            hexAction = StrategyQuery;
        }

        protected override void UpdateUIObject()
        {
            Entities.With(hexGroup).ForEach(hexAction);
        }

        void StrategyQuery(Entity entity,
                            ref HexBase.Component hex,
                            ref HexPower.Component power,
                            ref Position.Component position,
                            ref SpatialEntityId entityId)
        {
            SetHexObject(hex, entityId.EntityId, position);
        }

        private void SetHexObject(in HexBase.Component hex, in EntityId entityId, in Position.Component position)
        {
            var uiObject = GetUIObject(entityId, position.Coords, out var vec2, out var rot);
            uiObject.SetInfo(vec2, hex.Side, rot, HexSize);
        }

        float HexSize
        {
            get
            {
                return HexDictionary.HexEdgeLength * MiniMapUIDisplay.Instance.MiniMapRate;
            }
        }
    }
}
