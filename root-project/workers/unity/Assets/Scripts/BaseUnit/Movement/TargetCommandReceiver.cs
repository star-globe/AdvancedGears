using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class TargetCommandReceiver : MonoBehaviour
    {
        [Require] BaseUnitTargetCommandReceiver targetCommandReceiver;
        [Require] BaseUnitTargetWriter targetWriter;
        [Require] BaseUnitMovementReader movementReader;
        [Require] BaseUnitMovementWriter movementWriter;

        public void OnEnable()
        {
            targetCommandReceiver.OnSetTargetRequestReceived += OnSetTargetRequest;
            movementReader.OnBoidDiffedEvent += OnBoidDiffed;
        }

        private void OnSetTargetRequest(BaseUnitTarget.SetTarget.ReceivedRequest request)
        {
            targetCommandReceiver.SendSetTargetResponse(new BaseUnitTarget.SetTarget.Response(request.RequestId, new Empty()));

            targetWriter.SendUpdate(new BaseUnitTarget.Update()
            {
                TargetInfo = request.Payload,
            });
        }

        private void OnBoidDiffed(BoidVector vector)
        {
            movementWriter.SendUpdate(new BaseUnitMovement.Update()
            {
                BoidVector = vector.Vector,
            });
        }
    }
}
