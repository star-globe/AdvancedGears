using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class CoalObject : MonoBehaviour
    {
        int amount;
        long entityId = 0;
        ulong coalId = 0;
        CoalVanishEvent vanishEvent;

        public bool IsActive
        {
            get { return this.gameObject.activeSelf; }
            set
            {
                if (value != this.gameObject.activeSelf)
                    this.gameObjec.SetActive(value);
            }
        }

        public void SetCoal(int amount, long entityId, ulong coalId)
        {
            this.amount = amount;
            this.entityId = entityId;
            this.coalId = coalId;
        }

        public void SetCallback(CoalVanishEvent vanishEvent)
        {
            this.vanishEvent = vanishEvent;
        }

        public int Gather()
        {
            vanishEvent?.Invoke(entityId, coalId);
            return amount;
        }
    }
}
