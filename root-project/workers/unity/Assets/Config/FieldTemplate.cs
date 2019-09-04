using System;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public static class FieldTemplate
    {
        public static EntityTemplate CreateFieldEntityTemplate(Coordinates coords, float range, float highest, FieldMaterialType materialType = FieldMaterialType.None)
        {
            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(coords), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot("Ground"), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new FieldComponent.Snapshot { TerrainPoints = CreateTerrainPointInfo(range, highest, materialType) }, WorkerUtils.UnityGameLogic);

            template.SetReadAccess(WorkerUtils.AllWorkerAttributes.ToArray());
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            return template;
        }

        const float shrinkRate = 0.3f;
        public static List<TerrainPointInfo> CreateTerrainPointInfo(float range, float highest, FieldMaterialType materialType = FieldMaterialType.None)
        {
            List<TerrainPointInfo> list = new List<TerrainPointInfo>();
            int layer = (int)(highest * 10 / range) + 1;
            float lowest = 0.0f;
            float tileSize = 10.0f;
            for (int i = 0; i < layer; i++) {
                list.Add(new TerrainPointInfo
                        {
                            HighestHillHeight = highest,
                            LowestHillHeight = lowest,
                            TileSize = tileSize,
                            Seeds = UnityEngine.Random.Range(0,999),
                            Range = range,
                            MatType = materialType,
                        });

                highest *= shrinkRate;
                lowest = -highest;
                tileSize /= shrinkRate;
                range /= shrinkRate;
            }

            return list;
        }
    }
}
