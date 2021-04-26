using Improbable.Gdk.Core;
using AdvancedGears.Scripts.UI;
using Unity.Entities;

namespace AdvancedGears
{
    [Obsolete]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class UpdateUISystem : ComponentSystem
    {
        private ComponentUpdateSystem componentUpdateSystem;

        private EntityQuery launcherGroup;
        private EntityQuery scoreGroup;

        protected override void OnCreate()
        {
            base.OnCreate();

            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            launcherGroup = GetEntityQuery(
                ComponentType.ReadOnly<Launcher.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<Launcher.HasAuthority>()
            );

            scoreGroup = GetEntityQuery(
                ComponentType.ReadOnly<Score.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<Score.HasAuthority>()
            );
        }

        protected override void OnUpdate()
        {
            Entities.With(launcherGroup).ForEach(
                (ref SpatialEntityId spatialEntityId, ref Launcher.Component launcher) =>
                {
                    var spatialId = spatialEntityId.EntityId;
                    var launcherUpdates =
                        componentUpdateSystem.GetEntityComponentUpdatesReceived<Launcher.Update>(spatialId);
                    if (launcherUpdates.Count > 0)
                    {
                        UIComponent.Main.TestText.text = launcher.RechargeTimeLeft > 0.0f
                            ? "Recharging"
                            : $"Energy: {launcher.EnergyLeft}";
                    }
                });

            Entities.With(scoreGroup).ForEach((ref SpatialEntityId spatialEntityId, ref Score.Component score) =>
            {
                var spatialId = spatialEntityId.EntityId;
                var launcherUpdates =
                    componentUpdateSystem.GetEntityComponentUpdatesReceived<Score.Update>(spatialId);
                if (launcherUpdates.Count > 0)
                {
                    UIComponent.Main.ScoreText.text = $"Score: {score.Score}";
                }
            });
        }
    }
}
