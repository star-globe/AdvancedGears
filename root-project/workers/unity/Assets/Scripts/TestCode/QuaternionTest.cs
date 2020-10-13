using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.TransformSynchronization;
using AdvancedGears;

public class QuaternionTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var q = Quaternion.Euler(0, 90.0f, 0);

        Debug.Log($"Quaternion:{q.eulerAngles}");

        var qq = q.ToCompressedQuaternion();

        Debug.Log($"Quaternion:data:{qq.Data} EulerAnges{qq.ToUnityQuaternion().eulerAngles}");
    }

}
