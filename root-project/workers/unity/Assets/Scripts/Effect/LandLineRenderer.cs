using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    [RequireComponent(typeof(LineRenderer))]
    public class LandLineRenderer : MonoBehaviour
    {
        [SerializeField]
        BaseUnitStateColorSettings colorSettings;

        private LineRenderer lineRenderer;

        public LineRenderer Renderer
        {
            get
            {
                lineRenderer = lineRenderer ?? GetComponentInChildren<LineRenderer>();
                return lineRenderer;
            }
        }

        public void SetLines(UnitSide side, Vector3[] basePoints, int layerMask, int cutNumber, float fromHeight, float underHeight, float buffer)
        {
            if (basePoints == null)
                return;

            Vector3[] newPoints = null;
            if (cutNumber < 1)
                newPoints = basePoints;
            else {
                var list = new List<Vector3>();
                Vector3 point = Vector3.zero;
                for (var i = 0; i < basePoints.Length; i++) {
                    if (i == 0) {
                        point = basePoints[i];
                        list.Add(point);
                    }
                    else {
                        var end = basePoints[i];
                        foreach (var c in Enumerable.Range(1,cutNumber)) {
                            list.Add((end * c + point * (cutNumber - c)) / cutNumber);
                        }

                        point = end;
                    }
                }

                newPoints = list.ToArray();
            }

            for(var i = 0; i < newPoints.Length; i++) {
                var p = newPoints[i];
                if (Physics.Raycast(new Vector3(p.x, fromHeight, p.z), Vector3.down, out var hit, fromHeight - underHeight, layerMask:layerMask) == false)
                    continue;
                
                newPoints[i] = new Vector3(p.x, hit.point.y + buffer, p.z);
            }

            var col = colorSettings.GetSideColor(side);
            this.Renderer.startColor = col;
            this.Renderer.endColor = col;

            this.Renderer.positionCount = newPoints.Length;
            this.Renderer.SetPositions(newPoints);
        }
    }
}
