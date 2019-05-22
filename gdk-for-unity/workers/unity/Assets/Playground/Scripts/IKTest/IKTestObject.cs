using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playground
{
    public class IKTestObject : MonoBehaviour
    {
        [SerializeField]
        PosturePoint point;

        [SerializeField]
        TargetTracker target;

        [SerializeField]
        UnitTransform unit;

        PostureTransform posture = null;
        PostureTransform Posture
        {
            get
            {
                if (posture == null)
                {
                    unit.PostureDic.TryGetValue(point, out posture);
                }

                return posture;
            }
        }

        CannonTransform terminal = null;
        CannonTransform Terminal
        {
            get
            {
                if (terminal == null)
                {
                    terminal = unit.GetTerminal<CannonTransform>(point);
                }

                return terminal;
            }
        }

        void Update()
        {
            var pos = target.transform.position;
            if (Terminal == null)
                return;

            var diff = Terminal.transform.position - pos;
            if (diff.magnitude < 0.5f)
                return;

            this.Posture.Resolve(pos);
        }
    }
}
