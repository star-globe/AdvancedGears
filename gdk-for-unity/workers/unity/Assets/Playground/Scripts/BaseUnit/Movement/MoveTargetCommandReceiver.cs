using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Common;

namespace Playground
{
    public class MoveTargetCommandReceiver : MonoBehaviour
    {
        [Require] BaseUnitMovementCommandReceiver movementCommandReceiver;
        [Require] BaseUnitMovementWriter movementWriter;

        public void OnEnable()
        {
            movementCommandReceiver.OnSetTargetRequestReceived += OnSetTargetRequest;
        }

        private void OnSetTargetRequest(BaseUnitMovement.SetTarget.ReceivedRequest request)
        {
            movementCommandReceiver.SendSetTargetResponse(new BaseUnitMovement.SetTarget.Response(request.RequestId, new Empty()));

            movementWriter.SendUpdate(new BaseUnitMovement.Update()
            {
                TargetInfo = request.Payload,
            });
        }
    }
}
