using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/CommanderUnit Config/InitSettings", order = 1)]
    public class CommanderUnitInitSettings : ScriptableObject
    {
        [SerializeField] float sightRange = 100.0f;
        public float SightRange => sightRange;
        [SerializeField] float allyRange = 50.0f;
        public float AllyRange => allyRange;
        [SerializeField] float inter = 3.0f;
        public float Inter => inter;

        [SerializeField] float forwardLength = 10.0f;
        public float ForwardLength => forwardLength;

        [SerializeField] float separeteWeight = 1.0f;
        public float SepareteWeight => separeteWeight;
        [SerializeField] float alignmentWeight = 1.0f;
        public float AlignmentWeight => alignmentWeight;
        [SerializeField] float cohesionWeight = 1.0f;
        public float CohesionWeight => cohesionWeight;

        [SerializeField] float captureSpeed = 1.5f;
        public float CaptureSpeed => captureSpeed;
    }
}
