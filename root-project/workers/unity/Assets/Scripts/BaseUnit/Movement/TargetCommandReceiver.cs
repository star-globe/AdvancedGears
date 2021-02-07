using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class TargetCommandReceiver : MonoBehaviour
    {
        //[Require] BaseUnitTargetCommandReceiver targetCommandReceiver;
        [Require] BaseUnitTargetWriter targetWriter;
        [Require] BaseUnitSightReader sightReader;
        [Require] BaseUnitSightWriter sightWriter;

        public void OnEnable()
        {
            //targetCommandReceiver.OnSetTargetRequestReceived += OnSetTargetRequest;
            //targetCommandReceiver.OnSetFrontLineRequestReceived += OnSetFrontLineRequest;
            //targetCommandReceiver.OnSetHexRequestReceived += OnSetHexRequest;
            //targetCommandReceiver.OnSetStrongholdRequestReceived += OnSetStrongholdRequest;
            targetWriter.OnSetTargetEvent += OnSetTarget;
            targetWriter.OnSetFrontLineEvent += OnSetFrontLine;
            targetWriter.OnSetHexEvent += OnSetHex;
            sightReader.OnBoidDiffedEvent += OnBoidDiffed;
        }

        private void OnSetTarget(TargetInfo info)
        {
            targetWriter.SendUpdate(new BaseUnitTarget.Update()
            {
                TargetUnit = info.TgtInfo,
                PowerRate = info.PowerRate,
                Type = TargetType.Unit,
            });

            Debug.Log("OnSetTarget");
        }

        private void OnSetFrontLine(TargetFrontLineInfo lineInfo)
        {
            targetWriter.SendUpdate(new BaseUnitTarget.Update()
            {
                FrontLine = lineInfo.FrontLine,
                PowerRate = lineInfo.PowerRate,
                Type = TargetType.FrontLine,
            });

            Debug.LogFormat("OnSetFrontLine: Left:{0} Right{1}", lineInfo.FrontLine.LeftCorner, lineInfo.FrontLine.RightCorner);
        }

        private void OnSetHex(TargetHexInfo hexInfo)
        {
            targetWriter.SendUpdate(new BaseUnitTarget.Update()
            {
                HexInfo = hexInfo.HexInfo,
                PowerRate = hexInfo.PowerRate,
                Type = TargetType.Hex,
            });

            Debug.Log("OnSetHex");
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
