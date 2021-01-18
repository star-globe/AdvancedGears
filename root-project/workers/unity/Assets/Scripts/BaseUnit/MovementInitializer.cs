using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class MovementInitializer : MonoBehaviour
    {
        [Require] BaseUnitMovementWriter movement;

        [SerializeField]
        BaseUnitInitSettings settings;

        void Start()
        {
            Assert.IsNotNull(settings);

            movement.SendUpdate(new BaseUnitMovement.Update
            {
                MoveSpeed = settings.Speed,
                RotSpeed = settings.RotSpeed,
                ConsumeRate = settings.ConsumeRate,
            });
        }
    }
}
