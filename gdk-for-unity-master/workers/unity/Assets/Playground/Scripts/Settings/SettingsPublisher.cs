using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playground
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
        }
    }

    public interface ISettingsPublisher
    {
        void Publish();
    }
}
