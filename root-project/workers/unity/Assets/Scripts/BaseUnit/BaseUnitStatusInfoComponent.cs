using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class BaseUnitStatusInfoComponent : SpatialMonoBehaviour
    {
        [Require] BaseUnitStatusReader reader;
        

        public UnitType Type { get; private set; }
        public UnitSide Side { get; private set; }
        public OrderType Order { get; private set; }
        public UnitState State{ get; private set; }
        public uint Rank { get; private set; }
        public float Size { get; private set; }
        public EntityId EntityId { get; private set; }

        void Start()
        {
            var data = reader.Data;
            Type = data.Type;
            Side = data.Side;
            Order = data.Order;
            State = data.State;
            Rank = data.Rank;

            var comp = this.SpatialComp;
            if (comp != null)
                this.EntityId = comp.EntityId;

            var unit = GetComponent<UnitTransform>();
            Size = unit == null ? 0 : unit.SizeRadius;

            reader.OnUpdate += BaseUnitUpdate;
        }

        void BaseUnitUpdate(BaseUnitStatus.Update update)
        {
            if (update.Type.HasValue)
                Type = update.Type.Value;

            if (update.Side.HasValue)
                Side = update.Side.Value;

            if (update.Order.HasValue)
                Order = update.Order.Value;

            if (update.State.HasValue)
                State = update.State.Value;

            if (update.Rank.HasValue)
                Rank = update.Rank.Value;
        }
    }
}
