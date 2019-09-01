using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class HitNotifiersInfo : MonoBehaviour
    {
        [SerializeField]
        HitNotifier[] notifiers;

        public HitNotifier[] Notifiers { get { return notifiers; } }
    }
}
