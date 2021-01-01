using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class DrawHexGrid : MonoBehaviour
    {
        [SerializeField]
        int layerNumber;

        [SerializeField]
        float length = 100;

        [SerializeField]
        HexGridComponent baseHex;

        [SerializeField]
        RectTransform parent;

        const float edgeLength = 100;

        public void DrawGrid()
        {
            var comps = parent.GetComponentsInChildren<HexGridComponent>();

            var canvasSize = parent.rect.size.magnitude;
            var hexSize = canvasSize / (2 * layerNumber + 1);
            int total = layerNumber * (layerNumber + 1) * 3 + 1;
            for (uint i = 0; i < total; i++)
            {
                var center = HexUtils.GetHexCenter(Vector3.zero, i, edge: edgeLength);

                center *= hexSize / (edgeLength * 2);

                HexGridComponent grid = null;
                if (i < comps.Length)
                    grid = comps[i];
                else
                    grid = Instantiate(baseHex, parent);

                grid.gameObject.SetActive(true);
                grid.MyRect.localPosition = new Vector3(center.x, center.z, 0);

                var size = grid.MyRect.sizeDelta.x;
                grid.MyRect.localScale = Vector3.one * hexSize / size;

                grid.DrawGrid(i);
            }

            for (int i = total; i < comps.Length; i++)
            {
                comps[i].gameObject.SetActive(false);
            }
        }

        public void ReturnAll()
        {
            var comps = parent.GetComponentsInChildren<HexGridComponent>();
            for (var i = 0; i < comps.Length; i++)
            {
                Destroy(comps[i].gameObject);
                comps[i] = null;
            }
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(DrawHexGrid))]
    public class DrawHexGridEditor : UnityEditor.Editor
    {
        DrawHexGrid component = null;

        void OnEnable()
        {
            component = target as DrawHexGrid;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("DrawGrid"))
                component.DrawGrid();

            if (GUILayout.Button("ReturnAll"))
                component.ReturnAll();

        }
    }
#endif
}
