using System;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public static class FieldTemplate
    {
        public static EntityTemplate CreateFieldEntityTemplate(Coordinates coords, float range, float highest, FieldMaterialType materialType = FieldMaterialType.None, int? seeds = null)
        {
            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(coords), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot("Ground"), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new FieldComponent.Snapshot { TerrainPoints = CreateTerrainPointInfo(range, highest, materialType, seeds) }, WorkerUtils.UnityGameLogic);

            template.SetReadAccess(WorkerUtils.AllWorkerAttributes.ToArray());
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            return template;
        }

        const float heightRate = 10.0f;
        const float shrinkRate = 0.3f;
        const float initTile = 1.5f;
        public static List<TerrainPointInfo> CreateTerrainPointInfo(float range, float highest, FieldMaterialType materialType = FieldMaterialType.None, int? seeds = null)
        {
            List<TerrainPointInfo> list = new List<TerrainPointInfo>();
            int layer = (int)(highest * heightRate / range) + 1;
            float lowest = highest / 2;
            float tileSize = 1.5f;
            for (int i = 0; i < layer; i++) {
                list.Add(new TerrainPointInfo
                        {
                            HighestHillHeight = highest,
                            LowestHillHeight = lowest,
                            TileSize = tileSize,
                            Seeds = seeds == null ? UnityEngine.Random.Range(0,999): seeds.Value,
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
