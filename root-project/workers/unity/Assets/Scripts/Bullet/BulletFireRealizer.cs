using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;

namespace AdvancedGears
{
    public class BulletFireRealizer : BulletFireBase
    {
        [Require] BulletComponentReader reader;
        [Require] private World world;
        protected override World World => world;

        void OnEnable()
        {
            reader.OnFiresEvent += Fire;
            reader.OnVanishesEvent += Vanish;
        }

        private void Fire(BulletFireInfo info)
        {
            base.Creator?.OnFire(info);
        }

        private void Vanish(BulletVanishInfo info)
        {
            base.Creator?.OnVanish(info);
        }
    }

    public abstract class BulletFireBase : WorldInfoReader
    {
        BulletMovementSystem bulletSystem = null;
        BulletMovementSystem BulletSystem
        {
            get
            {
                if (World == null)
                    return null;

                bulletSystem = bulletSystem ?? World.GetExistingSystem<BulletMovementSystem>();
                return bulletSystem;
            }
        }

        protected BulletCreator Creator
        {
            get
            {
                if (this.BulletSystem == null)
                    return null;

                return this.BulletSystem.BulletCreator;
            }
        }

        CommandSystem commandSystem = null;
        CommandSystem CommandSystem
        {
            get
            {
                if (World == null)
                    return null;

                commandSystem = commandSystem ?? World.GetExistingSystem<CommandSystem>();
                return commandSystem;
            }
        }

        protected void CreateFlare(Vector3 pos, FlareColorType col, UnitSide side, float startTime)
        {
            var template = BulletTemplate.CreateFlareEntityTemplate(pos.ToWorldCoordinates(this.Origin), col, side, startTime);
            var request = new WorldCommands.CreateEntity.Request(template);
            this.CommandSystem.SendCommand(request);
        }
    }
}
