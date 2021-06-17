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
        private LineRenderer lineRenderer;
        private readonly Vector3[] linePoints = new Vector3[256];
        private readonly List<Vector3> pointList = new List<Vector3>();

        public LineRenderer Renderer
        {
            get
            {
                lineRenderer = lineRenderer ?? GetComponentInChildren<LineRenderer>();
                return lineRenderer;
            }
        }

        public void SetLinesColor(UnitSide side)
        {
            var col = ColorDictionary.GetSideColor(side);
            this.Renderer.startColor = col;
            this.Renderer.endColor = col;
        }

        public void SetLines(UnitSide side, Vector3[] basePoints, int layerMask, int cutNumber, float fromHeight, float underHeight, float buffer)
        {
            if (basePoints == null)
                return;

            int count = 0;
            if (cutNumber < 1) {
                SetPoints(linePoints, basePoints, out count);
            }
            else {
                pointList.Clear();
                Vector3 point = Vector3.zero;
                for (var i = 0; i < basePoints.Length; i++) {
                    if (i == 0) {
                        point = basePoints[i];
                        pointList.Add(point);
                    }
                    else {
                        var end = basePoints[i];

                        for (int j = 1; j <= cutNumber; j++) {
                            pointList.Add((end * j + point * (cutNumber - j)) / cutNumber);
                        }

                        point = end;
                    }
                }

                SetPoints(linePoints, pointList, out count);
            }

            for(var i = 0; i < count; i++) {
                var p = linePoints[i];
                if (Physics.Raycast(new Vector3(p.x, fromHeight, p.z), Vector3.down, out var hit, fromHeight - underHeight, layerMask:layerMask) == false)
                    continue;
                
                linePoints[i] = new Vector3(p.x, hit.point.y + buffer, p.z);
            }

            var col = ColorDictionary.GetSideColor(side);
            this.Renderer.startColor = col;
            this.Renderer.endColor = col;

            this.Renderer.positionCount = count;
            this.Renderer.SetPositions(linePoints);
        }

        private void SetPoints(Vector3[] points, Vector3[] basePoints, out int count)
        {
            count = -1;
            for (var i = 0; i < basePoints.Length; i++)
            {
                if (i >= points.Length) {
                    count = points.Length;
                    break;
                }

                points[i] = basePoints[i];
            }

            if (count < 0)
                count = basePoints.Length;
        }

        private void SetPoints(Vector3[] points, List<Vector3> pointList, out int count)
        {
            count = -1;
            for (var i = 0; i < pointList.Count; i++)
            {
                if (i >= points.Length) {
                    count = points.Length;
                    break;
                }

                points[i] = pointList[i];
            }

            if (count < 0)
                count = pointList.Count;
        }
    }
}
