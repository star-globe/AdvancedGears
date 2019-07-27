using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class TargetTracker : MonoBehaviour
    {
        Plane plane = new Plane();

        void Update()
        {
            if (Input.GetMouseButton(0) == false)
                return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var diff = (Camera.main.transform.position - this.transform.position).normalized;
            plane.SetNormalAndPosition(diff, this.transform.position);

            float enter;
            if (plane.Raycast(ray, out enter))
            {
                this.transform.position = ray.GetPoint(enter);
            }
        }
    }
}
