using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine.AI;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class SymbolicObject : MonoBehaviour
    {
        [SerializeField]
        Renderer objectRenderer;

        public void SetRendererPosScale(Vector3 pos, Vector3 scale)
        {
            objectRenderer.transform.position = pos;
            objectRenderer.transform.localScale = scale;
        }
    }
}
