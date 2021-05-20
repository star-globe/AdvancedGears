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
    public class TreeComponentRealizer : WorldInfoReader
    {
        [Require] World world;
        protected override World World => world;
        [Require] TreeComponentReader reader;

        [SerializeField] GameObject treeObjectBase;

        float inter = 1.0f;
        void Start()
        {
            var trees = reader.Data.Trees;
            foreach (var t in trees){
                var go = GameObject.Instantiate(treeObjectBase, this.transform);
                // = StaticObjectCreator.GetTree(t.TreeId);
                go.position = FieldUtils.GetGround(t.PosX, t.PosZ, inter, FieldDictionary.MaxHeight, this.Origin);
            }
        }
    }
}
