using System;
using System.IO;
using System.Collections.Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using UnityEditor;
using UnityEngine;
using Snapshot = Improbable.Gdk.Core.Snapshot;

namespace AdvancedGears.Editor
{
    public delegate float GetSnapshotHeight(float x, float y);

    public static class SnapshotGenerator
    {
    	public struct Arguments
        {
            public string OutputPath;
        }

        public static string DefaultSnapshotRelativePath = Path.Combine(
                "..",
                "..",
                "..",
                "snapshots",
                "default.snapshot");

        public static string GetDefaultSnapshotPath(string relativePath = null)
        {
            var path = Path.Combine(Application.dataPath, relativePath ?? DefaultSnapshotRelativePath);
            return Path.GetFullPath(path);
        }

        public static void Generate(Arguments arguments, GetSnapshotHeight ground = null)
        {
            Debug.Log("Generating snapshot.");
            var snapshot = CreateSnapshot(ground);

            Debug.Log($"Writing snapshot to: {arguments.OutputPath}");
            snapshot.WriteToFile(arguments.OutputPath);
        }

        public static void Generate(Arguments arguments, float fieldSize, GetSnapshotHeight ground = null, List<UnitSnapshot> units = null, List<FieldSnapshot> fields = null)
        {
            Debug.Log("Generating snapshot.");
            var snapshot = CreateSnapshot(ground, fieldSize, units, fields);

            Debug.Log($"Writing snapshot to: {arguments.OutputPath}");
            snapshot.WriteToFile(arguments.OutputPath);
        }

        const float standardSize = 400.0f;
        private static Snapshot CreateSnapshot(GetSnapshotHeight ground = null, float fieldSize = standardSize, List<UnitSnapshot> units = null, List<FieldSnapshot> fields = null)
        {
            var snapshot = new Snapshot();

            int count = (int)Mathf.Round(fieldSize / standardSize) * 2;
            for (int i = 0; i <= count; i++)
            {
                for (int j = 0; j <= count; j++)
                {
                    var length_x = standardSize * (i - (count - 1) / 2.0f);
                    var length_z = standardSize * (j - (count - 1) / 2.0f);
                    AddPlayerSpawner(snapshot, GroundCoordinates( length_x, length_z, ground));
                }
            }

            AddWorldTimer(snapshot, Coordinates.Zero);
            if (units == null)
                AddDefaultUnits(snapshot, ground);
            else
                AddUnits(snapshot, ground, units);

            if (fields == null)
                AddField(snapshot, Coordinates.Zero);
            else
                AddFields(snapshot, fields);

            return snapshot;
        }

        const float heightBuffer = 1.0f;

        private static Coordinates GroundCoordinates(double x, double z, GetSnapshotHeight ground)
        {
            double y = ground == null ?  0: (double)ground((float)x, (float)z);
            y += heightBuffer;
            return new Coordinates(x,y,z);
        }

        private static void AddPlayerSpawner(Snapshot snapshot, Coordinates playerSpawnerLocation)
        {
            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(playerSpawnerLocation), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot { EntityType = "PlayerCreator" }, WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new PlayerCreator.Snapshot(), WorkerUtils.UnityGameLogic);

            template.SetReadAccess(WorkerUtils.UnityGameLogic, WorkerUtils.UnityClient, WorkerUtils.MobileClient);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            snapshot.AddEntity(template);
        }

        private static void AddField(Snapshot snapshot, Coordinates location)
        {
            snapshot.AddEntity(FieldTemplate.CreateFieldEntityTemplate(location, 3000, 500, FieldMaterialType.None));
        }

        private static void AddFields(Snapshot snapshot, List<FieldSnapshot> fields)
        {
            foreach(var f in fields)
                snapshot.AddEntity(FieldTemplate.CreateFieldEntityTemplate(f.pos.ToCoordinates(), f.range, f.highest, f.materialType, f.seeds));
        }

        private static void AddWorldTimer(Snapshot snapshot, Coordinates location)
        {
            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(location), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot("WorldTimer"), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new WorldTimer.Snapshot(), WorkerUtils.UnityGameLogic);

            template.SetReadAccess(WorkerUtils.UnityGameLogic, WorkerUtils.UnityClient, WorkerUtils.MobileClient);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);
            snapshot.AddEntity(template);
        }

        static readonly double scale = 4.0;
        
        private static void AddCubeGrid(Snapshot snapshot, int cubeCount, GetSnapshotHeight ground = null)
        {
            // Calculate grid size
            var gridLength = (int) Math.Ceiling(Math.Sqrt(cubeCount));
            if (gridLength % 2 == 1) // To make sure nothing is in (0, 0)
            {
                gridLength += 1;
            }

            var cubesToSpawn = cubeCount;

            for (var x = -gridLength + 1; x <= gridLength - 1; x += 2)
            {
                for (var z = -gridLength + 1; z <= gridLength - 1; z += 2)
                {
                    // Leave the centre empty
                    if (x == 0 && z == 0)
                    {
                        continue;
                    }

                    // Exit when we've hit our cube limit
                    if (cubesToSpawn-- <= 0)
                    {
                        return;
                    }

                    UnitSide side = x < 0 ? UnitSide.A : UnitSide.B;
                    int nx;
                    if (x < 0)
                        nx = x-3;
                    else
                        nx = x+3;

                        double pos_x = nx * scale;
                    double pos_z = z * scale;
                    var entityTemplate = BaseUnitTemplate.CreateBaseUnitEntityTemplate(side, GroundCoordinates(pos_x, pos_z, ground), UnitType.Soldier);
                    snapshot.AddEntity(entityTemplate);
                }
            }

            AddDefaultUnits(snapshot, ground);
        }

        private static void AddUnits(Snapshot snapshot, GetSnapshotHeight ground, List<UnitSnapshot> units)
        {
            foreach(var u in units)
            {
                var template = BaseUnitTemplate.CreateBaseUnitEntityTemplate(u.side, GroundCoordinates(u.pos.x, u.pos.z, ground), u.type);
                snapshot.AddEntity(template);
            }
        }

        private static void AddDefaultUnits(Snapshot snapshot, GetSnapshotHeight ground = null)
        {
            var gridLength = (int)Math.Ceiling(Math.Sqrt(16));
            var len = gridLength * scale;
            var templateA = BaseUnitTemplate.CreateBaseUnitEntityTemplate(UnitSide.A, GroundCoordinates(-len * 3, 0, ground), UnitType.Stronghold);
            var templateB = BaseUnitTemplate.CreateBaseUnitEntityTemplate(UnitSide.B, GroundCoordinates(len * 3, 0, ground), UnitType.Stronghold);
            snapshot.AddEntity(templateA);
            snapshot.AddEntity(templateB);

            var templateCa = BaseUnitTemplate.CreateBaseUnitEntityTemplate(UnitSide.A, GroundCoordinates(-len * 2, 0, ground), UnitType.Commander);
            var templateCb = BaseUnitTemplate.CreateBaseUnitEntityTemplate(UnitSide.B, GroundCoordinates(len * 2, 0, ground), UnitType.Commander);
            snapshot.AddEntity(templateCa);
            snapshot.AddEntity(templateCb);

            var templateHa = BaseUnitTemplate.CreateBaseUnitEntityTemplate(UnitSide.A, GroundCoordinates(-len * 3.5, 0, ground), UnitType.HeadQuarter);
            var templateHb = BaseUnitTemplate.CreateBaseUnitEntityTemplate(UnitSide.B, GroundCoordinates(len * 3.5, 0, ground), UnitType.HeadQuarter);
            snapshot.AddEntity(templateHa);
            snapshot.AddEntity(templateHb);
        }

        private static void CreateSpinner(Snapshot snapshot, Coordinates coords)
        {
            const string entityType = "Spinner";

            var transform = Improbable.Gdk.TransformSynchronization.TransformUtils.CreateTransformSnapshot(coords.ToUnityVector(), Quaternion.identity);

            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(coords), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot(entityType), WorkerUtils.UnityGameLogic);
            template.AddComponent(transform, WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Collisions.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new SpinnerColor.Snapshot(Color.BLUE), WorkerUtils.UnityGameLogic);
            template.AddComponent(new SpinnerRotation.Snapshot(), WorkerUtils.UnityGameLogic);

            template.SetReadAccess(WorkerUtils.UnityGameLogic, WorkerUtils.UnityClient, WorkerUtils.MobileClient);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            snapshot.AddEntity(template);
        }
    }

    static class EditorExtensions
    {
        public static float GetHeight(this TerrainCollider ground, float x, float z, float maxHeight = 1000.0f)
        {
            var ray = new Ray(new Vector3(x, maxHeight, z), Vector3.down);
            RaycastHit hit;
            if (ground.Raycast(ray, out hit, maxHeight))
            {
                return hit.point.y;
            }
            else
            {
                return 0;
            }
        }
    }
}
