using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class TankTargetTracer : MonoBehaviour
    {
        [SerializeField]
        GameObject sourceObject;

        [SerializeField]
        Transform centerAxis;

        [SerializeField]
        float rotSpeed = 30.0f;

        [SerializeField]
        float pitchSpeed = 3.0f;

        float radius = 5.0f;
        const float inTime = 0.001f;

        void FixedUpdate()
        {
            var current = centerAxis.InverseTransformPoint(this.transform.position);
            var target = centerAxis.InverseTransformPoint(sourceObject.transform.position);

            current = current.normalized * radius;
            target = target.normalized * radius;

            var diff = target - current;

            float x = 0, y = 0, z= 0;

            float checkSpeed = inTime * pitchSpeed;
            if (diff.y * diff.y > checkSpeed * checkSpeed)
            {
                y = CalcDiff(diff.y, pitchSpeed);
            }

            //　回転を考慮した計算にすること

            checkSpeed = inTime * rotSpeed;
            if (diff.x * diff.x + diff.z * diff.z > checkSpeed * checkSpeed)
            {
                x = CalcDiff(diff.x, rotSpeed);
                z = CalcDiff(diff.z, rotSpeed);
            }

            if (x == 0 && y == 0 && z == 0)
                return;

            var delta = new Vector3(x,y,z);
            var newPosition = current + delta;

            newPosition = newPosition.normalized * radius;
            newPosition = centerAxis.TransformPoint(newPosition);

            this.transform.position = newPosition;
        }

        private float CalcDiff(float diff, float rotSpeed)
        {
            return Mathf.Sign(diff) * rotSpeed * Mathf.Deg2Rad * radius * Time.deltaTime;
        }
    }
}
