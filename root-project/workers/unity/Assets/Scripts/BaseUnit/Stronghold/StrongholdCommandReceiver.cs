using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class StrongholdCommandReceiver : MonoBehaviour
    {
        [Require] BaseUnitStatusReader statusReader;
        [Require] StrongholdSightWriter sightWriter;

        public void OnEnable()
        {
            sightWriter.OnSetStrategyVectorEvent += OnSetStrategyVectorCommanderRequest;
        }

        private void OnSetStrategyVectorCommanderRequest(StrategyVectorEvent vectorEvent)
        {
            var side = statusReader.Data.Side;
            if (side != vectorEvent.FromSide)
            {
                Debug.LogWarningFormat("StrongholdCommandReceiver:SideError Side:{0}", vectorEvent.FromSide);
                return;
            }

            sightWriter.SendUpdate(new StrongholdSight.Update()
            {
                StrategyVector = vectorEvent.StrategyVector,
            });


            //Debug.LogFormat("StrongholdCommandReceiver:SetVector:{0} Side:{1} EntityId:{2}", request.Payload.Vector.ToUnityVector(), request.Payload.Side, request.EntityId);
        }
    }
}
