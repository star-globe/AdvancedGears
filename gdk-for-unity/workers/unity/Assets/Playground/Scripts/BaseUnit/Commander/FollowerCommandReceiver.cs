using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Common;

namespace Playground
{
    public class FollowerCommandReceiver : MonoBehaviour
    {
        [Require] CommanderStatusCommandReceiver commanderCommandReceiver;
        [Require] CommanderStatusWriter commanderWriter;

        public void OnEnable()
        {
            commanderCommandReceiver.OnAddFollowerRequestReceived += OnAddFollowerRequest;
        }

        private void OnAddFollowerRequest(CommanderStatus.AddFollower.ReceivedRequest request)
        {
            commanderCommandReceiver.SendAddFollowerResponse(new CommanderStatus.AddFollower.Response(request.RequestId, new Empty()));

            commanderWriter.SendUpdate(new CommanderStatus.Update()
            {
                FollowerInfo = request.Payload,
            });
        }
    }
}
