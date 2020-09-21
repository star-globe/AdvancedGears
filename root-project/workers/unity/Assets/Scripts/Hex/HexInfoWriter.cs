using System.Collections;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Entities;
using UnityEngine;
using TMPro;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class HexInfoWriter : MonoBehaviour
    {
        [Require] HexBaseWriter writer;

        private void OnEnable()
        {
            writer.OnSideChangedEvent += SideChangedEvent;
        }

        void SideChangedEvent(SideChangedEvent sideChanged)
        {
            writer.SendUpdate(new HexBase.Update()
            {
                Side = sideChanged.Side,
            });
        }
    }
}
