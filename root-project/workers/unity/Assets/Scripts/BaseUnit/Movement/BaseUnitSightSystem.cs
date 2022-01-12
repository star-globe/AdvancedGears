using System.Collections.Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class BaseUnitSightSystem : BaseSearchSystem
    {
        class VectorContainer
        {
            public Vector3? boidTarget;
            public Vector3 spread;
        }

        EntityQuery movementGroup;
        EntityQuery boidGroup;

        IntervalChecker intervalMovement;
        IntervalChecker intervalBoid;

        EntityQueryBuilder.F_EDD<BaseUnitStatus.Component, SpatialEntityId> boidQuery;
        EntityQueryBuilder.F_EDDDDD<MovementData, NavPathData, BaseUnitSight.Component, BaseUnitStatus.Component, SpatialEntityId> movementQuery;

        double deltaTime = -1.0;

        const int periodMovement = 20;
        const int periodBoid = 2;

        readonly Dictionary<EntityId, VectorContainer> vectorDic = new Dictionary<EntityId, VectorContainer>();

        protected override void OnCreate()
        {
            base.OnCreate();

            movementGroup = GetEntityQuery(
                ComponentType.ReadOnly<UnitTransform>(),
                ComponentType.ReadWrite<MovementData>(),
                ComponentType.ReadWrite<NavPathData>(),
                ComponentType.ReadOnly<BaseUnitSight.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            boidGroup = GetEntityQuery(
                 ComponentType.ReadOnly<Transform>(),
                 ComponentType.ReadOnly<BaseUnitStatus.Component>()
            );

            intervalMovement = IntervalCheckerInitializer.InitializedChecker(periodMovement);
            intervalBoid = IntervalCheckerInitializer.InitializedChecker(periodBoid);

            deltaTime = Time.ElapsedTime;

            boidQuery = BoidQuery;
            movementQuery = MovementQuery;
        }

        protected override void OnUpdate()
        {
            UpdateBoid();
            UpdateMovement();
        }

        int navCount = 0;

        private void UpdateMovement()
        {
            if (CheckTime(ref intervalMovement) == false)
                return;

            deltaTime = Time.ElapsedTime - deltaTime;
            Entities.With(movementGroup).ForEach(movementQuery);

            deltaTime = Time.ElapsedTime;
        }

        private void MovementQuery(Entity entity,
                                          ref MovementData movement,
                                          ref NavPathData path,
                                          ref BaseUnitSight.Component sight,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId)
        {
            movement.MoveSpeed = 0.0f;
            movement.RotSpeed = 0.0f;

            if (status.State != UnitState.Alive)
                return;

            if (UnitUtils.IsAutomaticallyMoving(status.Type) == false)
                return;

            var unit = EntityManager.GetComponentObject<UnitTransform>(entity);

            // check ground
            //if (unit == null || unit.GetGrounded(out var hitInfo) == false)
            //    return;

            if (sight.State == TargetState.None)
                return;

            var trans = unit.transform;
            var pos = trans.position;

            Vector3? tgt = null;
            Vector3 spread = Vector3.zero;

            var id = entityId.EntityId;
            if (vectorDic.ContainsKey(id)) {
                //tgt = vectorDic[id].boidTarget;
                spread = vectorDic[id].spread;
            }

            if (tgt == null)
                tgt = sight.GetTargetPosition(this.Origin, pos);

            tgt = CheckNavPathAndTarget(tgt.Value, pos, unit.SizeRadius, sight.State, entityId.EntityId.Id, ref path);

            if (RangeDictionary.IsSpreadValid(spread)) {
                var length = (tgt.Value - pos).magnitude;
                tgt += spread * Mathf.Max(1.0f, (length / RangeDictionary.SpreadSize));
            }

            var positionDiff = tgt.Value - pos;

            var forward = MovementUtils.get_forward(positionDiff, sight.TargetRange, trans.forward);

            MovementDictionary.TryGet(status.Type, out var speed, out var rot);

            var isRotate = rotate(rot, trans, positionDiff);

            if (forward != 0.0f)
                movement.MoveSpeed = forward * speed;

            if (isRotate != 0)
                movement.RotSpeed = rot * isRotate;
        }

        private void UpdateBoid()
        {
            if (CheckTime(ref intervalBoid) == false)
                return;

            var keys = vectorDic.Keys;
            foreach (var k in keys)
            {
                var container = vectorDic[k];
                container.boidTarget = null;
                container.spread = Vector3.zero;
            }

            Entities.With(boidGroup).ForEach(boidQuery);
        }

        private void BoidQuery(Entity entity,
                              ref BaseUnitStatus.Component status,
                              ref SpatialEntityId entityId)
        {
            if (status.State != UnitState.Alive)
                return;

            if (UnitUtils.IsAutomaticallyMoving(status.Type) == false)
                return;

            var trans = EntityManager.GetComponentObject<Transform>(entity);

            // check ground
            if (trans == null)
                return;

            var pos = trans.position;

            Vector3? tgt = null;//calc_update_boid(ref sight, sight.State, pos);
            Vector3 spread = Vector3.zero;

            var range = RangeDictionary.SpreadSize;
            var bodySize = RangeDictionary.BodySize;
            var units = getAllUnits(pos, range, allowDead: true);
            foreach (var u in units)
            {
                var diff = pos - u.pos;
                var mag = Mathf.Max(bodySize, diff.magnitude);

                spread += diff.normalized * ((range / mag) - 1.0f) * bodySize;
            }

            if (units.Count > 0)
                spread /= units.Count;

            var id = entityId.EntityId;
            if (vectorDic.ContainsKey(id))
            {
                var container = vectorDic[id];
                container.boidTarget = tgt;
                container.spread = spread;
            }
            else
            {
                vectorDic[entityId.EntityId] = new VectorContainer() { boidTarget = tgt, spread = spread };
            }
        }

        #region method
        /// <summary>
        /// check in range
        /// </summary>
        /// <param name="forward"></param>
        /// <param name="tgt"></param>
        /// <param name="range"></param>
        /// <param name="rot"></param>
        /// <returns></returns>
        bool in_range(Vector3 forward, Vector3 tgt, float range, out Vector3 rot)
        {
            rot = Vector3.Cross(forward, tgt);

            if (Vector3.Dot(forward, tgt) < 0.0f)
                return false;

            return Mathf.Asin(rot.magnitude) < Mathf.Deg2Rad * range;
        }

        /// <summary>
        /// get rotate info
        /// </summary>
        /// <param name="rotSpeed"></param>
        /// <param name="trans"></param>
        /// <param name="diff"></param>
        /// <returns></returns>
        int rotate(float rotSpeed, Transform trans, Vector3 diff)
        {
            var rate = MovementDictionary.RotateLimitRate;
            return MovementUtils.rotate(trans, diff, rotSpeed * Time.DeltaTime, rate, out var is_over);
        }

        /// <summary>
        /// calculate target and update the boid info;
        /// </summary>
        /// <param name="sight"></param>
        /// <param name="targetState"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        Vector3? calc_update_boid(ref BaseUnitSight.Component sight, TargetState targetState, Vector3 pos)
        {
            Vector3? tgt = null;
            var boidVector = sight.BoidVector;

            if (boidVector.Potential > 0.0f)
            {
                var center = boidVector.Center.ToWorkerPosition(this.Origin);

                if ((center - pos).sqrMagnitude > boidVector.SqrtBoidRadius())
                    tgt = center;
                else if (targetState == TargetState.OutOfRange)
                    tgt = pos + boidVector.GetVector3(sight.TargetRange);
            }

            var current = Time.ElapsedTime;
            var diffTime = (float)(current - sight.BoidUpdateTime);
            boidVector.Potential = AttackLogicDictionary.ReduceBoidPotential(boidVector.Potential, diffTime);
            sight.BoidUpdateTime = current;
            sight.BoidVector = boidVector;

            return tgt;
        }

        NavPathContainer navPathContainer = new NavPathContainer();
        //const float checkRange = 1.0f;
        //readonly Dictionary<long, Vector3[]> pointsDic = new Dictionary<long, Vector3[]>();

        Vector3 CheckNavPathAndTarget(Vector3 target, Vector3 current, float size, TargetState state, long uid, ref NavPathData path)
        {
            return navPathContainer.CheckNavPathAndTarget(target, current, size, uid, WalkableNavArea, ref path);
            //if (pointsDic.ContainsKey(uid) == false)
            //    pointsDic[uid] = new Vector3[256];
            //
            //var points = pointsDic[uid];
            //if (path.IsSetData == false || (target - path.target).sqrMagnitude > checkRange * checkRange)
            //{
            //    NavMeshUtils.GetNavPoint(current, target, size, WalkableNavArea, out var count, points);
            //    if (count > 0)
            //    {
            //        path.count = count;
            //        path.current = 0;
            //        path.target = target;
            //        return path.GetCurrentCorner(points);
            //    }
            //}
            //else
            //{
            //    if ((path.GetCurrentCorner(points) - current).sqrMagnitude < size * size)
            //    {
            //        path.Next();
            //    }
            //
            //    return path.GetCurrentCorner(points);
            //}
            //
            //return target;
        }
        #endregion
    }
}
