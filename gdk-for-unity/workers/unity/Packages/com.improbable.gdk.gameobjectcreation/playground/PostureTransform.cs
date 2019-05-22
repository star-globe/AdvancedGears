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

        [SerializeField] ConnectorTransform root;

        AttachedTransform[] connectors = null;
        public AttachedTransform[] Connectors { get { return conectors; } }
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
        bool IsSet { get { return connectors != null; } }

        Start()
        {
            Assert.IsNotNull(root);

            if (IsSet == false)
                CheckConnectors();
        }

        public void CheckConnectors()
        {
                var list = new List<AttachedTransform>(root);

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

                connectors = list.ToArray();//list.OrderBy(c => c.transform.GetSiblingIndex()).ToArray();
        }

        public void SetQuaternion(int index, Quaternion quo)
        {
            if (IsSet == false)
                return;

            if (index < 0 || index >= connectors.Length)
                return;

            connectors[index].transform.rotation = quo;
        }

        public void Resolve(Vector3 position)
        {
            if (this.IsSet == false)
                return;

            int length = connectors.Length;
            if (length == 0)
                return;

            if (length == 1)
            {
                SetAndGetDummyPosition(connectors[0],null,position);
                return;
            }

            Vector3 dmy = SetAndGetDummyPosition(connetctors[length-2], connectors[length-1], position);
            for (int j = length -3; j > 0; j--)
            {
                dmy = SetAndGetDummyPosition(conntectors[j], connectors[j+1], dmy);
            }
            for (int i = 0; i < length -2; i++)
            {
                dmy = SetAndGetDummyPosition(conntectors[i+1], connectors[i], dmy);
            }
        }

        Vector3 SetAndGetDummyPosition(AttachedTransform attached, AttachedTransform next, Vector3 tgt)
        {
            var foward = (tgt - attached.transform.position).normalized;
            RotateLogic.Rotate(attached.transform, attached.HingeAxis, foward);
            if (next == null)
                return Vector3.zero;

            return attached.position + (tgt - next.position);
        }

        static void CheckChildren<T>(T tgt, List<T> list) where T : Component
        {
            var child = tgt.gameObject.GetComponentInChildren();
            if (child == null)
                return;

            foreach (var c in children)
            {
                list.Add(c);
                CheckChildren(c, list);
            }
        }
    }
}
