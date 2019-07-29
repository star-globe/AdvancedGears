using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedGears
{
    public static class RandomLogic
    {
        public static Vector3 XZRandomCirclePos(float radius, float rate = 0.4f)
        {
            var rad = UnityEngine.Random.Range(0,Mathf.PI*2.0f);
            var range = radius * (1.0f + UnityEngine.Random.Range(0,rate) - rate/2);

            return new Vector3(range * Mathf.Cos(rad) , 0.0f, range * Mathf.Sin(rad));
        }
    }
}

