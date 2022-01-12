using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.QueryBasedInterest;
using UnityEngine;
using System.Collections.Generic;

namespace AdvancedGears
{
    public static class SnapshotUtils
    {
        public delegate float GetSnapshotHeight(float x, float y);

        const float standardSize = 400.0f;
        public static Snapshot GenerateGroundSnapshot(float fieldSize, GetSnapshotHeight ground = null)
        {
            var snapshot = new Snapshot();

            int count = (int) Mathf.Round(fieldSize / standardSize) * 2;
            for (int i = 0; i <= count; i++)
            {
                for (int j = 0; j <= count; j++)
                {
                    var length_x = standardSize * (i - (count - 1) / 2.0f);
                    var length_z = standardSize * (j - (count - 1) / 2.0f);
                    AddPlayerSpawner(snapshot, GroundCoordinates(length_x, length_z, ground));
                }
            }

            return snapshot;
        }

        private static void AddPlayerSpawner(Snapshot snapshot, Coordinates playerSpawnerLocation)
        {
            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(playerSpawnerLocation), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot { EntityType = "PlayerCreator" }, WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new PlayerCreator.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new WorldTimer.Snapshot(), WorkerUtils.UnityGameLogic);

            var query = InterestQuery.Query(Constraint.RelativeCylinder(500));
            var interest = InterestTemplate.Create()
                .AddQueries<Position.Component>(query);
            template.AddComponent(interest.ToSnapshot(), WorkerUtils.UnityGameLogic);

            template.SetReadAccess(WorkerUtils.UnityGameLogic, WorkerUtils.UnityClient, WorkerUtils.MobileClient);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);
            snapshot.AddEntity(template);
        }

        const float heightBuffer = 1.0f;
        public static Coordinates GroundCoordinates(double x, double y, double z, bool NeedBuffer)
        {
            y += NeedBuffer ? heightBuffer: 0;
            return new Coordinates(x, y, z);
        }

        public static Coordinates GroundCoordinates(double x, double z, GetSnapshotHeight ground)
        {
            double y = ground == null ? 0 : (double) ground((float) x, (float) z);
            y += heightBuffer;
            return new Coordinates(x, y, z);
        }

        const float receiverSize = 800;
        public static void AddLongRangeBulletReciever(float fieldSize, Snapshot snapshot)
        {
#if false
            int count = (int) Mathf.Round(fieldSize / standardSize) * 2;
            for (int i = 0; i <= count; i++)
            {
                for (int j = 0; j <= count; j++)
                {
                    var length_x = standardSize * (i - (count - 1) / 2.0f);
                    var length_z = standardSize * (j - (count - 1) / 2.0f);

                    snapshot.AddEntity(CreateLongRangeBulletRecieverTemplate(new Coordinates(length_x, 0, length_z)));
                }
            }
#else
            snapshot.AddEntity(CreateLongRangeBulletRecieverTemplate(Coordinates.Zero));
#endif
        }

        private static EntityTemplate CreateLongRangeBulletRecieverTemplate(Coordinates coords)
        {
            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(coords), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot { EntityType = "LongRangeBulletReciever" }, WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new StrategyLongBulletReceiver.Snapshot{ CurrentBullets = new List<BulletFireInfo>() }, WorkerUtils.UnityGameLogic);
            return template;
        }
    }
}
