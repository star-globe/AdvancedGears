using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Common;

namespace Playground
{
    public class FollowerCommandReceiver : MonoBehaviour
    {
        [Require] CommanderStatusCommandReceiver commandReceiver;
        [Require] CommanderStatusWriter writer;

        public void OnEnable()
        {
            commandReceiver.OnAddFollowerRequestReceived += OnAddFollowerRequest;
        }

        private void OnAddFollowerRequest(CommanderStatus.AddFollower.ReceivedRequest request)
        {
            commandReceiver.SendAddFollowerResponse(new CommanderStatus.AddFollower.Response(request.RequestId, new Empty()));

            writer.SendUpdate(new CommanderStatus.Update()
            {
                FollowerInfo = request.Payload,
            });
        }

        private void OnSetSuperiorRequest(CommanderStatus.SetSuperior.ReceivedRequest request)
        {
            commandReceiver.SendAddFollowerResponse(new CommanderStatus.SetSuperior.Response(request.RequestId, new Empty()));

            writer.SendUpdate(new CommanderStatus.Update()
            {
                SuperiorInfo = request.Payload,
            });
        }
    }
}
