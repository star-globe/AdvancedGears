using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;
using UnityEngine;

namespace AdvancedGears
{
    public class RootPostureInitializer : MonoBehaviour
    {
        [Require]
        PostureRootReader reader;

        void OnEnable()
        {
            var root = reader.Data.RootTrans;
            var quo = root.Rotation.ToUnityQuaternion();

            Debug.Log($"angle:{quo.eulerAngles}");

            this.transform.rotation = root.Rotation.ToUnityQuaternion();
            this.transform.localScale = root.Scale.ToUnityVector();
        }
    }

}
