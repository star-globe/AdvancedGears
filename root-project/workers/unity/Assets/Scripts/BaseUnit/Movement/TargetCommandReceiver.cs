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
        [Require] BaseUnitSightReader sightReader;
        [Require] BaseUnitSightWriter sightWriter;

        public void OnEnable()
        {
            targetCommandReceiver.OnSetTargetRequestReceived += OnSetTargetRequest;
            targetCommandReceiver.OnSetFrontLineRequestReceived += OnSetFrontLineRequest;
            targetCommandReceiver.OnSetHexRequestReceived += OnSetHexRequest;
            sightReader.OnBoidDiffedEvent += OnBoidDiffed;
        }

        private void OnSetTargetRequest(BaseUnitTarget.SetTarget.ReceivedRequest request)
        {
            targetCommandReceiver.SendSetTargetResponse(new BaseUnitTarget.SetTarget.Response(request.RequestId, new Empty()));

            targetWriter.SendUpdate(new BaseUnitTarget.Update()
            {
                TargetInfo = request.Payload,
                Type = TargetType.Unit,
            });

            //Debug.Log("OnSetTarget");
        }

        private void OnSetFrontLineRequest(BaseUnitTarget.SetFrontLine.ReceivedRequest request)
        {
            targetCommandReceiver.SendSetFrontLineResponse(new BaseUnitTarget.SetFrontLine.Response(request.RequestId, new Empty()));

            var line = request.Payload;
            targetWriter.SendUpdate(new BaseUnitTarget.Update()
            {
                FrontLine = request.Payload,
                Type = TargetType.FrontLine,
            });

            //Debug.LogFormat("OnSetFrontLine: Left:{0} Right{1}", line.FrontLine.LeftCorner, line.FrontLine.RightCorner);
        }

        private void OnSetHexRequest(BaseUnitTarget.SetHex.ReceivedRequest request)
        {
            targetCommandReceiver.SendSetHexResponse(new BaseUnitTarget.SetHex.Response(request.RequestId, new Empty()));

            targetWriter.SendUpdate(new BaseUnitTarget.Update()
            {
                HexInfo = request.Payload,
                Type = TargetType.Hex,
            });

            //Debug.Log("OnSetHex");
        }

        private void OnBoidDiffed(BoidVector vector)
        {
            sightWriter.SendUpdate(new BaseUnitSight.Update()
            {
                BoidVector = vector,
                BoidUpdateTime = Time.time,
            });
        }
    }
}
