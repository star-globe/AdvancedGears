using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playground
{
    public class CompoundBulletReceiver : DynamicBulletReceiver
    {
        [SerializeField]
        HitNotifier[] notifiers;

        void Start()
        {
            foreach (var n in notifiers)
            {
                if (n != null)
                    n.OnCollisionEvent += OnCollisionEnter;
            }
        }

        void OnDestroy()
        {
            foreach (var n in notifiers)
            {
                if (n != null)
                    n.OnCollisionEvent -= OnCollisionEnter;
            }
        }
    }
}

