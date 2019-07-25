using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    public class StaticBulletReceiver : BulletHitReceiver
    {
        World world;
        protected override World World => world;

        public void SetWorld(World world)
        {
            this.world = world;
        }
    }
}
