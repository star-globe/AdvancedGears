using System;
using System.Collections;
using System.Collections.Generic;
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
        IntervalChecker interClient;
        private Unity.Entities.EntityQuery group;
        EntityQueryBuilder.F_EDC<PlayerInfo.Component, Transform> action;
        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadOnly<PlayerInfo.Component>(),
                ComponentType.ReadOnly<Transform>()
            );

            interClient = IntervalCheckerInitializer.InitializedChecker(this.IntervalTime, setChecked: true);

            action = Query;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (CheckTime(ref interClient) == false)
                return;

            Entities.With(group).ForEach(action);
        }    
                
        private void Query(Unity.Entities.Entity entity,
                            ref PlayerInfo.Component playerInfo,
                            Transform transform)
        {
            if (playerInfo.ClientWorkerId.Equals(this.WorkerSystem.WorkerId) == false)
                return;

            var pos = transform.position - this.Origin;
            playerPosition = new Vector3(pos.x, 0, pos.z);
        }

        public void SetXZPosition(float x, float z)
        {
            this.playerPosition = new Vector3(x,0,z);
        }
    }

    public abstract class FieldQueryBaseSystem : EntityQuerySystem
    {
        //float checkRange;
        //Vector3? checkedPosition = null;

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
                if (this.BasePosition != null)//checkedPosition != null && BasePosition != null)
                {
                    //var diff = checkedPosition.Value - BasePosition.Value;
                    //if (diff.sqrMagnitude < checkRange * checkRange)
                    //    return false;

                    if (FieldCreator.CheckNeedRealize(this.BasePosition.Value) == false)
                        return false;

                    DebugUtils.LogFormatColor(UnityEngine.Color.red, "BasePosition:{0}", BasePosition.Value);
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
        }

        protected override void OnUpdate()
        {
            if (this.BasePosition == null)
                return;

            base.OnUpdate();
        }

        protected override void SendEntityQuery()
        {
            base.SendEntityQuery();

            //DebugUtils.LogFormatColor(UnityEngine.Color.magenta, "SendFieldQuery. WorkerId:{0}", this.WorkerSystem.WorkerId);
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
                SetField(shots);
            }
            else
            {
                SetFieldClear();
            }
        }

        private void SetField(Dictionary<EntityId, List<EntitySnapshot>> shots)
        {
            FieldCreator.Reset();

            int snapShotCount = 0;
            foreach (var kvp in shots)
            {
                foreach (var snap in kvp.Value)
                {
                    Position.Snapshot position;
                    if (snap.TryGetComponentSnapshot(out position) == false)
                        continue;

                    FieldComponent.Snapshot field;
                    if (snap.TryGetComponentSnapshot(out field) == false)
                        continue;

                    FieldCreator.RealizeField(field.TerrainPoints, position.Coords, this.BasePosition);
                    snapShotCount++;
                }
            }

            DebugUtils.LogFormatColor(UnityEngine.Color.red,"FieldWorkerType:{0} RealizeSnapShot.Count:{1}",
                                      this.FieldWorkerType, snapShotCount);

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


