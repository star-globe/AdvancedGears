using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using AdvancedGears;

namespace AdvancedGears.Editor
{
    public class HexUtilsTest : MonoBehaviour
    {
        [SerializeField]
        uint targetIndex = 0;

        public void CalcNeighborIndexes()
        {
            var ids = HexUtils.GetNeighborHexIndexes(targetIndex);

            string str = string.Empty;
            foreach (var i in ids)
            {
                str += string.Format("{0}, ",i);
            }

            Debug.LogFormat("TargetIndex:{0} Neighbors:{1}", targetIndex, str);
        }
    }
}
