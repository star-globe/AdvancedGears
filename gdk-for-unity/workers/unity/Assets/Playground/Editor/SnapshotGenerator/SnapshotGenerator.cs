using System;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.PlayerLifecycle;
using Improbable.Transform;
using UnityEngine;
using Quaternion = Improbable.Transform.Quaternion;

namespace Playground.Editor.SnapshotGenerator
{
    internal static class SnapshotGenerator
    {
        public struct Arguments
        {
            public int NumberEntities;
            public string OutputPath;
        }

        public static void Generate(Arguments arguments)
        {
            Debug.Log("Generating snapshot.");
            var snapshot = CreateSnapshot(arguments.NumberEntities);

            Debug.Log($"Writing snapshot to: {arguments.OutputPath}");
            snapshot.WriteToFile(arguments.OutputPath);
        }

        private static Snapshot CreateSnapshot(int cubeCount, TerrainCollider ground = null)
        {
            var snapshot = new Snapshot();

            AddPlayerSpawner(snapshot, GroundCoordinates( 2000, 2000, ground));//new Coordinates(2000, 0, 2000));
            AddPlayerSpawner(snapshot, GroundCoordinates( 2000,-2000, ground));//new Coordinates(2000, 0, -2000));
            AddPlayerSpawner(snapshot, GroundCoordinates(-2000,-2000, ground));//new Coordinates(-2000, 0, -2000));
            AddPlayerSpawner(snapshot, GroundCoordinates(-2000, 2000, ground));//new Coordinates(-2000, 0, 2000));

            AddCubeGrid(snapshot, cubeCount);
            //CreateSpinner(snapshot, new Coordinates { X = 5.5, Y = 0.5f, Z = 0.0 });
            //CreateSpinner(snapshot, new Coordinates { X = -5.5, Y = 0.5f, Z = 0.0 });

            return snapshot;
        }

        private static Coordinates GroundCoordinates(int x, int z, TerrainCollider ground)
        {
            var y = ground == null ?  0: (int)ground.GetHeight(x,z);
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

        static readonly double scale = 4.0;
        
        private static void AddCubeGrid(Snapshot snapshot, int cubeCount, TerrainCollider ground = null)
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

            var len = gridLength * scale;
            var templateA = BaseUnitTemplate.CreateBaseUnitEntityTemplate(UnitSide.A, GroundCoordinates(-len * 3, 0, ground),UnitType.Stronghold);
            var templateB = BaseUnitTemplate.CreateBaseUnitEntityTemplate(UnitSide.B, GroundCoordinates( len * 3, 0, ground),UnitType.Stronghold);
            snapshot.AddEntity(templateA);
            snapshot.AddEntity(templateB);
            
            var templateCa = BaseUnitTemplate.CreateBaseUnitEntityTemplate(UnitSide.A, GroundCoordinates(-len * 2, 0, ground), UnitType.Commander);
            var templateCb = BaseUnitTemplate.CreateBaseUnitEntityTemplate(UnitSide.B, GroundCoordinates( len * 2, 0, ground), UnitType.Commander);
            snapshot.AddEntity(templateCa);
            snapshot.AddEntity(templateCb);
        }

        private static void CreateSpinner(Snapshot snapshot, Coordinates coords)
        {
            const string entityType = "Spinner";

            var transform = new TransformInternal.Snapshot
            {
                Location = new Location((float) coords.X, (float) coords.Y, (float) coords.Z),
                Rotation = new Quaternion(1, 0, 0, 0),
                TicksPerSecond = 1f / Time.fixedDeltaTime
            };

            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot { Coords = coords }, WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot { EntityType = entityType }, WorkerUtils.UnityGameLogic);
            template.AddComponent(transform, WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Collisions.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new SpinnerColor.Snapshot { Color = Color.BLUE }, WorkerUtils.UnityGameLogic);
            template.AddComponent(new SpinnerRotation.Snapshot(), WorkerUtils.UnityGameLogic);

            template.SetReadAccess(WorkerUtils.UnityGameLogic, WorkerUtils.UnityClient, WorkerUtils.MobileClient);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);


            snapshot.AddEntity(template);
        }
    }
}
