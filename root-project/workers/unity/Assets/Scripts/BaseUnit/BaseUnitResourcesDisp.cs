using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using TMPro;

namespace AdvancedGears
{
    public class BaseUnitResourcesDisp : MonoBehaviour
    {
        [Require] BaseUnitHealthReader healthReader;
        [Require] FuelComponentReader fuelReader;
        [Require] GunComponentReader gunReader;

        [SerializeField]
        TextMeshProUGUI resourceInfoText;

        ValueTuple<char,int,int> healthTuple;
        ValueTuple<char,int,int> fuelTuple;
        Dictionary<PosturePoint,ValueTuple<int,int>> gunTupleDic;
        char header = 'G';

        private void OnEnable()
        {
            healthReader.OnUpdateHealth += UpdateHealth;
            healthTuple.Item1 = 'H';
            healthTuple.Item2 = healthReader.Data.health;
            healthTuple.Item3 = healthReader.Data.MaxHealth;

            fuelReader.OnUpdateFuel += UpdateFuel;
            fuelTuple.Item1 = 'F';
            fuelTuple.Item2 = fuelReader.Data.health;
            fuelTuple.Item3 = fuelReader.Data.MaxHealth;
            
            gunReader.OnUpdateGunsDic += UpdateGuns;

            UpdateState(reader.Data.State);
            UpdateSide(reader.Data.Side);
        }

        void UpdateHealth(int health)
        {
            healthTuple.Item2 = health;
            UpdateInfo();
        }
        void UpdateFuel(int fuel)
        {
            fuelTuple.Item2 = fuel;
            UpdateInfo();
        }

        void UpdateGuns(Dictionary<PosturePoint,GunInfo> gunsDic)
        {
            foreach(var kvp in gunsDic) {
                if (gunTupleDic.ContainsKey(kvp.Key))
                    gunTupleDic[kvp.Key].Item1 = kvp.Value.StockBullets;
                else {
                    gunTupleDic.Add(kvp.Key, (kvp.Value.StockBullets, kvp.Value.StockMax));
                }
            }

            UpdateInfo();
        }

        const string fmt = "{0}:{1}:{2}\n";
        void UpdateInfo()
        {
            string content = string.Empty;
            content += string.Format(fmt, healthTuple.Item1, healthTuple.Item2, healthTuple.Item3);
            content += string.Format(fmt, fuelTuple.Item1, fuelTuple.Item2, fueldTuple.Item3);

            foreach(var kvp in gunTupleDic) {
                content += header + string.Format(fmt, kvp.Key, kvp.Value.Item1, kvp.Value.Item2);
            }

           resourceInfoText.SetText(content);
        }
    }
}
