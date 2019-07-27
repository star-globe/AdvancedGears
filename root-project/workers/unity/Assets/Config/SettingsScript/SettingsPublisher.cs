using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class SettingsPublisher : MonoBehaviour
    {
        private void Awake()
        {
            foreach (var component in GetComponentsInParent<MonoBehaviour>())
            {
                if (component is ISettingsPublisher publisher)
                {
                    publisher.Publish();
                }
            }

            Physics.gravity *= 1.0f / 5.0f;
        }
    }

    public interface ISettingsPublisher
    {
        void Publish();
    }
}
