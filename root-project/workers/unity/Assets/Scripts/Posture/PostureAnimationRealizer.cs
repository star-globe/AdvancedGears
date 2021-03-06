using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class PostureAnimationRealizer : MonoBehaviour
    {
        [Require] BoneAnimationReader reader;

        [SerializeField] PostureBoneContainer container;

        private void OnEnable()
        {
            reader.OnBoneMapUpdate += UpdateBone;
        }

        private void UpdateBone(Dictionary<int,CompressedLocalTransform> map)
        {
            container.SetTrans(map);
        }
    }
}
