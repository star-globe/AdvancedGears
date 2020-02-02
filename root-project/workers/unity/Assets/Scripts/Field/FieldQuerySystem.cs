using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Worker.CInterop;
using Improbable.Worker.CInterop.Query;
using ImprobableEntityQuery = Improbable.Worker.CInterop.Query.EntityQuery;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class FieldQueryServerSystem : FieldQueryBaseSystem
    {
        protected override Vector3? BasePosition => this.WorkerSystem.Origin;
        protected override bool CheckRegularly => false;
        protected override FieldWorkerType FieldWorkerType => FieldWorkerType.GameLogic;
    }

    [DisableAutoCreation]
    [AlwaysUpdateSystemAttribute]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class FieldQueryClientSystem : FieldQueryBaseSystem
    {
        Vector3? playerPosition = null;
        protected override Vector3? BasePosition => playerPosition;
        protected override bool CheckRegularly => true;
        protected override FieldWorkerType FieldWorkerType => FieldWorkerType.Client;
        IntervalChecker inter;
        private Unity.Entities.EntityQuery group;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadOnly<PlayerInfo.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(this.IntervalTime, setChecked: true);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (inter.CheckTime())
            {
                Entities.With(group).ForEach((Unity.Entities.Entity entity,
                                              ref PlayerInfo.Component playerInfo,
                                              ref Position.Component position) =>
                {
                    if (playerInfo.ClientWorkerId.Equals(this.WorkerSystem.WorkerId) == false)
                        return;

                    var pos = position.Coords.ToUnityVector();
                    playerPosition = new Vector3(pos.x, 0, pos.z);//position.Coords.ToUnityVector() + this.Origin;
                });
            }
        }

        public void SetXZPosition(float x, float z)
        {
            this.playerPosition = new Vector3(x,0,z);
        }
    }

    public abstract class FieldQueryBaseSystem : EntityQuerySystem
    {
        float checkRange;
        Vector3? checkedPosition = null;

        public FieldCreator FieldCreator { get; private set; }

        protected abstract Vector3? BasePosition { get; }
        protected abstract bool CheckRegularly { get; }
        protected abstract FieldWorkerType FieldWorkerType { get;}
        protected override float IntervalTime
        {
            get { return Settings == null ? 1.0f: Settings.UpdateInterval; }
        }

        public FieldSettings Settings
        {
            get { return FieldDictionary.Get(FieldWorkerType); }
        }

        protected override bool IsCheckTime
        {
            get { return CheckRegularly == false && FieldCreator.IsSetDatas; }
        }

        protected override bool OtherCheck
        {
            get
            {
                if (checkedPosition != null && BasePosition != null)
                {
                    var diff = checkedPosition.Value - BasePosition.Value;
                    if (diff.sqrMagnitude < checkRange * checkRange)
                        return false;

                    if (FieldCreator.CheckNeedRealize(this.BasePosition.Value) == false)
                        return false;

                    DebugUtils.LogFormatColor(UnityEngine.Color.red, "BasePosition:{0} CheckedPosition:{1} Diff:{2}",
                                              BasePosition.Value, checkedPosition.Value, diff);
                }

                return  true;
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            var go = new GameObject("FieldCreator");
            FieldCreator = go.AddComponent<FieldCreator>();
            FieldCreator.Setup(this.WorkerSystem.World, this.WorkerSystem.Origin, this.WorkerSystem.WorkerId, FieldWorkerType);

            var settings = FieldCreator.Settings;
            checkRange = settings != null ? settings.FieldSize / 2 : 0;
            checkRange *= FieldDictionary.CheckRangeRate;
        }

        protected override void OnUpdate()
        {
            if (this.BasePosition == null)
                return;

            base.OnUpdate();
        }

        protected override void SendEntityQuery()
        {
            if (BasePosition == null)
                checkedPosition = null;
            else
                checkedPosition = BasePosition.Value;

            base.SendEntityQuery();

            DebugUtils.LogFormatColor(UnityEngine.Color.magenta, "SendFieldQuery. WorkerId:{0}", this.WorkerSystem.WorkerId);
        }

        protected override ImprobableEntityQuery EntityQuery
        {
            get
            {
                var pos = BasePosition != null ? BasePosition.Value: Vector3.zero;
                var list = new IConstraint[]
                {
                    new ComponentConstraint(FieldComponent.ComponentId),
                    new SphereConstraint(pos.x, pos.y, pos.z, FieldDictionary.QueryRange),
                };

                return new ImprobableEntityQuery()
                {
                    Constraint = new AndConstraint(list),
                    ResultType = new SnapshotResultType()
                };
            }
        }

        protected override void ReceiveSnapshots(Dictionary<EntityId, List<EntitySnapshot>> shots)
        {
            if (shots.Count > 0)
            {
                SetField(shots.SelectMany(kvp => kvp.Value).ToList());
            }
            else
            {
                SetFieldClear();
            }
        }

        private void SetField(List<EntitySnapshot> snapShots)
        {
            FieldCreator.Reset();

            foreach (var snap in snapShots)
            {
                Position.Snapshot position;
                if (snap.TryGetComponentSnapshot(out position) == false)
                    continue;

                FieldComponent.Snapshot field;
                if (snap.TryGetComponentSnapshot(out field) == false)
                    continue;

                FieldCreator.RealizeField(field.TerrainPoints, position.Coords, this.BasePosition);
            }

            DebugUtils.LogFormatColor(UnityEngine.Color.red,"FieldWorkerType:{0} snapShots.Count:{1}",
                                      this.FieldWorkerType, snapShots.Count);

            FieldCreator.RemoveFields();
        }

        private void SetFieldClear()
        {
            FieldCreator.Reset();
            FieldCreator.RealizeEmptyField(this.BasePosition);
            FieldCreator.RemoveFields();
        }
    }
}


