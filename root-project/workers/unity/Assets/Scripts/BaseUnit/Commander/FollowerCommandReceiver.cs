using System.Collections;
using System.Linq;
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

        class FollowerInfoContainer
        {
            readonly List<EntityId> followers = new List<EntityId>();
            readonly List<EntityId> underCommanders = new List<EntityId>();

            IEnumerable<List<EntityId>> allLists
            {
                get
                {
                    yield return followers;
                    yield return underCommanders;
                }
            }

            public bool IsNeedToUpdate
            {
                get { return allLists.Any(l => l.Count > 0); }
            }

            public void AddFollowerInfo(in FollowerInfo info)
            {
                followers.AddRange(info.Followers);
                underCommanders.AddRange(info.UnderCommanders);
            }

            public void SetFollowers(ref FollowerInfo info)
            {
                info.SetFollowers(followers,underCommanders);
            }

            public void Clear()
            {
                foreach (var l in allLists)
                    l.Clear();
            }
        }

        readonly FollowerInfoContainer infoContainer = new FollowerInfoContainer();

        public void OnEnable()
        {
            commandReceiver.OnAddFollowerRequestReceived += OnAddFollowerRequest;
        }

        private void Update()
        {
            if (infoContainer.IsNeedToUpdate == false)
                return;

            var info = writer.Data.FollowerInfo;
            infoContainer.SetFollowers(ref info);

            writer.SendUpdate(new CommanderStatus.Update()
            {
                FollowerInfo = info,
            });

            infoContainer.Clear();
        }

        private void OnAddFollowerRequest(CommanderStatus.AddFollower.ReceivedRequest request)
        {
            commandReceiver.SendAddFollowerResponse(new CommanderStatus.AddFollower.Response(request.RequestId, new Empty()));

            infoContainer.AddFollowerInfo(request.Payload);
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
