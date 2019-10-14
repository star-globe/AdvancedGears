using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class HQCommandReceiver : MonoBehaviour
    {
        //[Require] HeadQuartersCommandReceiver commandReceiver;
        //[Require] HeadQuartersWriter writer;
        [Require] CommandersManagerCommandReceiver commandReceiver;
        [Require] CommandersManagerWriter writer;

        public void OnEnable()
        {
            //commandReceiver.OnAddOrderRequestReceived += OnAddFollowerOrderRequest;
            commandReceiver.OnAddCommanderRequestReceived += OnAddCommanderRequest;
        }

        //private void OnAddFollowerOrderRequest(HeadQuarters.AddOrder.ReceivedRequest request)
        //{
        //    commandReceiver.SendAddOrderResponse(new HeadQuarters.AddOrder.Response(request.RequestId, new Empty()));
        //
        //    var list = writer.Data.Orders;
        //    list.Add(request.Payload);
        //    writer.SendUpdate(new HeadQuarters.Update()
        //    {
        //        Orders = list,
        //    });
        //}

        private void OnAddCommanderRequest(CommandersManager.AddCommander.ReceivedRequest request)
        {
            commandReceiver.SendAddCommanderResponse(new CommandersManager.AddCommander.Response(request.RequestId, new Empty()));

            var datas = writer.Data.CommanderDatas;
            foreach(var id in request.Payload.Commanders)
                datas.Add(id, new TeamInfo(0,UnitState.None,null,null));

            writer.SendUpdate(new CommandersManager.Update()
            {
                CommanderDatas = datas,
                State = CommanderManagerState.None,
            });
        }
    }
}
