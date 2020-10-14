using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class PostureBonesContainer : MonoBehaviour
    {
        [Serializable]
        public class HashTransform
        {
            public int hash;
            public Transform transform;
        }

        [SerializeField] List<HashTransform> bones;

        public void SetTrans(PostureTransData postureData)
        {
            var index = bones.FindIndex(b => b.hash == postureData.BoneHash);
            if (index >= 0 && index < bones.Count)
            {
                bones[index].transform.position = postureData.Trans.Position.ToUnityVector();
                bones[index].transform.rotation = postureData.Trans.Rotation.ToUnityQuaternion();
                bones[index].transform.localScale = postureData.Trans.Scale.ToUnityvector();
            }
        }

        public void CheckBonesHash()
        {
            if (bones == null)
                return;

            var hashSet = new HashSet<int>();
            for (var i = 0; i < bones.Length; i++)
            {
                var t = bones[i].transform;
                if (t == null) {
                    Debug.LogError($"Transform is null! index:{i}");
                    break;
                }
                
                var hash = t.name.GetHashCode();
                if (hashSet.Add(hash) == false) {
                    Debug.LogError($"Dupulicated Hash ! index:{i} hash:{hash}");
                    break;
                }

                bones[i].hash = hash;
            }
        }
    }
}
