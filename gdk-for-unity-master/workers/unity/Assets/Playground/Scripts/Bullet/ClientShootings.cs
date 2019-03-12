using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.GameObjectRepresentation;

namespace Playground
{
    public class ClientShootings : MonoBehaviour
    {
        [Require] BulletComponent.Requirable.Writer writer;

        public void OnFire()
        {
            var fire = new BulletFireInfo()
            {
                LaunchTime = Time.realtimeSinceStartup,
            };

            writer.SendFires(fire);
        }
    }
}
