using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
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

            var info = writer.Data.FollowerInfo;
            info.SetFollowers(request.Payload.Followers, request.Payload.UnderCommanders);

            writer.SendUpdate(new CommanderStatus.Update()
            {
                FollowerInfo = info,
            });
        }

        private void OnSetSuperiorRequest(CommanderStatus.SetSuperior.ReceivedRequest request)
        {
            commandReceiver.SendSetSuperiorResponse(new CommanderStatus.SetSuperior.Response(request.RequestId, new Empty()));

            writer.SendUpdate(new CommanderStatus.Update()
            {
                SuperiorInfo = request.Payload,
            });
        }
    }
}
