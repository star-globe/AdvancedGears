using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectRepresentation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using UnityEngine.Experimental.PlayerLoop;

namespace Playground
{
    //[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    //public class BulletFireSystem : ComponentSystem
    //{
    //    private struct Data
    //    {
    //        public readonly int Length;
    //        [ReadOnly] public ComponentDataArray<BulletComponent.ReceivedEvents.Fires> Shots;
    //    }
    //
    //    [Inject] private Data data;
    //
    //    protected override void OnUpdate()
    //    {
    //        for (var i = 0; i < data.Length; i++)
    //        {
    //            foreach (var shot in data.Shots[i].Events)
    //            {
    //                var shotInfo = shot;
    //            }
    //        }
    //    }
    //}
}
