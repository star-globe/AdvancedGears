using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class StrongholdCommandReceiver : MonoBehaviour
    {
        [Require] StrongholdSightCommandReceiver commandReceiver;
        [Require] StrongholdSightWriter writer;

        public void OnEnable()
        {
            commandReceiver.OnSetStrategyVectorRequestReceived += OnSetStrategyVectorCommanderRequest;
        }

        private void OnSetStrategyVectorCommanderRequest(StrongholdSight.SetStrategyVector.ReceivedRequest request)
        {
            commandReceiver.SendSetStrategyVectorResponse(new StrongholdSight.SetStrategyVector.Response(request.RequestId, new Empty()));

            writer.SendUpdate(new StrongholdSight.Update()
            {
                StrategyVector = request.Payload,
            });


            //Debug.LogFormat("StrongholdCommandReceiver:SetVector:{0} EntityId:{1}", request.Payload.Vector.ToUnityVector(), request.EntityId);
        }
    }
}
