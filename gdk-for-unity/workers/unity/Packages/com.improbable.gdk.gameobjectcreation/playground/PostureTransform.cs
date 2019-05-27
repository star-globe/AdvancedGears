using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;
using Improbable.Common;

namespace Playground
{
    public class PostureTransform : MonoBehaviour
    {
        [SerializeField] PosturePoint point;
        public PosturePoint Point{ get { return point; } }

        [SerializeField] AttachedTransform[] connectors = null;

        public AttachedTransform[] Connectors { get { return connectors; } }
        public AttachedTransform TerminalAttached
        {
            get
            {
                int length = connectors == null ? 0: connectors.Length;
                if (length == 0)
                    return null;

                return connectors[length -1];
            }
        }
        public bool IsSet { get { return connectors != null; } }

        void Start()
        {
        }

        /*
        public void CheckConnectors()
        {
                var list = new List<AttachedTransform>();
                list.Add(root);

                CheckChildren<AttachedTransform>(root, list);

                ConnectorTransform src = root;
                foreach (var item in list)
                {
                    if (src == item)
                        continue;

                    if (src.Attached != null)
                        break;

                    src.SetAttach(item);

                    src = item as ConnectorTransform;
                    if (src == null)
                        break;
                }

                connectors = list.OrderBy(c => c.transform.GetSiblingIndex()).ToArray();
        }
        */

        public Quaternion[] GetQuaternions()
        {
            if (IsSet == false)
                return new Quaternion[0];

            return connectors.Select(c => c.transform.rotation).ToArray();
        }
        public void SetQuaternion(int index, Quaternion quo)
        {
            if (IsSet == false)
                return;

            if (index < 0 || index >= connectors.Length)
                return;

            connectors[index].transform.rotation = quo;
        }

        static void CheckChildren<T>(T tgt, List<T> list) where T : Component
        {
            var children = tgt.gameObject.GetComponentsInChildren<T>();
            var child = children.FirstOrDefault(c => c != tgt);
            if (child == null)
                return;

            list.Add(child);
            CheckChildren(child, list);
        }
    }
}
