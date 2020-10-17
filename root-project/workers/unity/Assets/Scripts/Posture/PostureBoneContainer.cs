using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class PostureBoneContainer : MonoBehaviour
    {
        [Serializable]
        public class HashTransform
        {
            public int hash;
            public Transform transform;
        }

        [SerializeField] List<HashTransform> bones;
        public List<HashTransform> Bones { get { return bones;}}

        Dictionary<int, CannonTransform> cannonDic = null;
        public Dictionary<int, CannonTransform> CannonDic
        {
            get
            {
                if (cannonDic == null)
                {
                    cannonDic = new Dictionary<int, CannonTransform>();
                    if (bones != null)
                    {
                        foreach (var b in bones)
                        {
                            var cannon = b.transform.GetComponent<CannonTransform>();
                            if (cannon != null)
                                cannonDic[b.hash] = cannon;
                        }
                    }
                }

                return cannonDic;
            }
        }

        public void SetTrans(PostureTransData postureData)
        {
            var hashTrans = GetHashTransform(postureData.BoneHash);
            if (hashTrans != null)
            {
                hashTrans.transform.position = postureData.Trans.Position.ToUnityVector();
                hashTrans.transform.rotation = postureData.Trans.Rotation.ToUnityQuaternion();
                hashTrans.transform.localScale = postureData.Trans.Scale.ToUnityVector();
            }
        }

        public CannonTransform GetCannon(int bone)
        {
            if (this.CannonDic.ContainsKey(bone))
                return CannonDic[bone];
            else
                return null;
        }

        public void SetTrans(int hash, CompressedLocalTransform trans)
        {
            var hashTrans = GetHashTransform(hash);
            if (hashTrans != null)
            {
                hashTrans.transform.position = trans.Position.ToUnityVector();
                hashTrans.transform.rotation = trans.Rotation.ToUnityQuaternion();
                hashTrans.transform.localScale = trans.Scale.ToUnityVector();
            }

        }

        public void SetTrans(Dictionary<int,CompressedLocalTransform> boneMap)
        {
            if (boneMap != null)
            {
                foreach (var kvp in boneMap)
                    SetTrans(kvp.Key, kvp.Value);
            }
        }

        private HashTransform GetHashTransform(int hash)
        {
            var index = bones.FindIndex(b => b.hash == hash);
            if (index >= 0 && index < bones.Count)
            {
                return bones[index];
            }
            else
                return null;
        }

        public void CheckBonesHash()
        {
            if (bones == null)
                return;

            var hashSet = new HashSet<int>();
            for (var i = 0; i < bones.Count; i++)
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
