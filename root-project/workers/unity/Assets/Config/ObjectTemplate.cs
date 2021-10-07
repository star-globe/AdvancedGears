using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.QueryBasedInterest;
using Improbable.Worker;

namespace AdvancedGears
{
    public class ObjectTemplate
    {
        static readonly Dictionary<ObjectType, string> metaDic = new Dictionary<ObjectType, string>()
        {
            { ObjectType.Tree, "Tree"},
            { ObjectType.Rock, "Rock"},
            { ObjectType.Building, "Building"},
            { ObjectType.Bridge, "Bridge"},
            { ObjectType.Tower, "Tower"},
            { ObjectType.Resource, "Resource"},
        };

        public static EntityTemplate CreateObjectEntityTemplate(ObjectType type, Coordinates coords, CompressedQuaternion? rotation = null, FixedPointVector3? scale = null)
        {
            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(coords), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot(metaDic[type]), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new PostureRoot.Snapshot() { RootTrans = PostureUtils.ConvertTransform(coords, rotation, scale) }, WorkerUtils.UnityGameLogic);

            SwitchType(template, type, WorkerUtils.UnityGameLogic);

            template.SetReadAccess(GetReadAttributes(type));
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            return template;
        }

        private static string[] GetReadAttributes(ObjectType type)
        {
            switch (type)
            {
                case ObjectType.Bridge:
                case ObjectType.Wall:
                case ObjectType.Tower:
                    return WorkerUtils.AllWorkerAttributes;

                default:
                    return WorkerUtils.AllPhysicalAttributes;
            }
        }

        private static void SwitchType(EntityTemplate template, ObjectType type, string writeAccess)
        {
            switch (type) {
                case ObjectType.Tree:
                    template.AddComponent(new TreeComponent.Snapshot { Trees = new List<TreeInfo>()}, writeAccess);
                    break;

                case ObjectType.Tower:
                    template.AddComponent(new SymbolicTower.Snapshot(), writeAccess);
                    break;
            }
        }

        public static EntityTemplate CreateTowerTemplate(UnitSide side, float height, Coordinates coords, CompressedQuaternion? rotation = null, FixedPointVector3? scale = null)
        {
            var template = CreateObjectEntityTemplate(ObjectType.Tower, coords, rotation, scale);
            var tower = template.GetComponent<SymbolicTower.Snapshot>();
            if (tower != null) {
                var t = tower.Value;
                t.Side = side;
                t.Height = height;
                template.SetComponent(t);
            }

            return template;
        }
    }
}
