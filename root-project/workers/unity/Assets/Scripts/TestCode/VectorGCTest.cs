using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VectorGCTest : MonoBehaviour
{
    Vector3 baseVec = Vector3.one;

    // Update is called once per frame
    void Update()
    {
        for (var i = 0; i < 1000; i++)
        {
            baseVec += new Vector3(3.0f, 4.0f, 5.0f);
        }

        baseVec = Vector3.one;
    }
}
