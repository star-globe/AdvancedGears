using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class BaseUnitResourcesDisp : MonoBehaviour
    {
        [Require] BaseUnitHealthReader healthReader;
        [Require] FuelComponentReader fuelReader;
        [Require] GunComponentReader gunReader;

        [SerializeField]
        Text resourceInfoText;

        ValueTuple<char,int,int> healthTuple;
        ValueTuple<char,int,int> fuelTuple;
        Dictionary<int,ValueTuple<int,int>> gunTupleDic;
        char header = 'G';

        private void OnEnable()
        {
            healthReader.OnHealthUpdate += UpdateHealth;
            healthTuple.Item1 = 'H';
            healthTuple.Item2 = healthReader.Data.Health;
            healthTuple.Item3 = healthReader.Data.MaxHealth;

            fuelReader.OnFuelUpdate += UpdateFuel;
            fuelTuple.Item1 = 'F';
            fuelTuple.Item2 = fuelReader.Data.Fuel;
            fuelTuple.Item3 = fuelReader.Data.MaxFuel;
            
            gunReader.OnGunsDicUpdate += UpdateGuns;

            UpdateHealth(healthReader.Data.Health);
            UpdateFuel(fuelReader.Data.Fuel);
            UpdateGuns(gunReader.Data.GunsDic);
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

        void UpdateGuns(Dictionary<int,GunInfo> gunsDic)
        {
            foreach(var kvp in gunsDic) {
                gunTupleDic[kvp.Key] = (kvp.Value.StockBullets, kvp.Value.StockMax);
            }

            UpdateInfo();
        }

        const string fmt = "{0}:{1}:{2}\n";
        void UpdateInfo()
        {
            string content = string.Empty;
            content += string.Format(fmt, healthTuple.Item1, healthTuple.Item2, healthTuple.Item3);
            content += string.Format(fmt, fuelTuple.Item1, fuelTuple.Item2, fuelTuple.Item3);

            foreach(var kvp in gunTupleDic) {
                content += header + string.Format(fmt, kvp.Key, kvp.Value.Item1, kvp.Value.Item2);
            }

           resourceInfoText.text = content;
        }
    }
}
