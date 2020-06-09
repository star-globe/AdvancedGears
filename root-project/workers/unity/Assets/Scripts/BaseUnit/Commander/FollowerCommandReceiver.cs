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
        [Require] CommanderTeamCommandReceiver commandReceiver;
        [Require] CommanderTeamWriter writer;
        [Require] CommanderStatusReader reader;
        [Require] BaseUnitStatusReader statusReader;

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
            commandReceiver.OnGetTeamInfoRequestReceived += OnTeamInfoRequest;
            commandReceiver.OnSetTargetStrogholdRequestReceived += OnSetStrongholdRequest;
            commandReceiver.OnSetTargetFrontlineRequestReceived += OnSetFrontlineRequest;
        }

        private void Update()
        {
            if (infoContainer.IsNeedToUpdate == false)
                return;

            var info = writer.Data.FollowerInfo;
            infoContainer.SetFollowers(ref info);

            writer.SendUpdate(new CommanderTeam.Update()
            {
                FollowerInfo = info,
            });

            infoContainer.Clear();
        }

        private void OnAddFollowerRequest(CommanderTeam.AddFollower.ReceivedRequest request)
        {
            commandReceiver.SendAddFollowerResponse(new CommanderTeam.AddFollower.Response(request.RequestId, new Empty()));

            infoContainer.AddFollowerInfo(request.Payload);
        }

        private void OnSetSuperiorRequest(CommanderTeam.SetSuperior.ReceivedRequest request)
        {
            commandReceiver.SendSetSuperiorResponse(new CommanderTeam.SetSuperior.Response(request.RequestId, new Empty()));

            writer.SendUpdate(new CommanderTeam.Update()
            {
                SuperiorInfo = request.Payload,
            });
        }

        private void OnTeamInfoRequest(CommanderTeam.GetTeamInfo.ReceivedRequest request)
        {
            var data = writer.Data;
            var status = statusReader.Data;

            //var hqId = data.HqInfo.EntityId;
            //if (request.EntityId != hqId)
            //{
            //    commandReceiver.SendGetTeamInfoFailure(request.RequestId, string.Format("Requested Wrong HQ EntityId:{0}", hqId.Id));
            //    return;
            //}
            //
            //commandReceiver.SendGetTeamInfoResponse(new CommanderTeam.GetTeamInfo.Response(request.RequestId, new TeamInfoResponse()
            //{
            //    HqEntityId = hqId,
            //    TeamInfo = new TeamInfo()
            //    {
            //        Rank = reader.Data.Rank,
            //    }
            //}));
        }

        private void OnSetStrongholdRequest(CommanderTeam.SetTargetStroghold.ReceivedRequest request)
        {
            writer.SendUpdate(new CommanderTeam.Update()
            {
                TargetStronghold = request.Payload,
                StrongholdEntityId = new EntityId(request.RequestId),
            });
        }

        private void OnSetFrontlineRequest(CommanderTeam.SetTargetFrontline.ReceivedRequest request)
        {
            writer.SendUpdate(new CommanderTeam.Update()
            {
                TargetFrontline = request.Payload,
                StrongholdEntityId = new EntityId(request.RequestId),
            });
        }
    }
}
