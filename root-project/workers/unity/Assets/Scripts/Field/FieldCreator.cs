using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Entities;
using Improbable;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class FieldCreator : MonoBehaviour
    {
        World world;
        Vector3 Origin;

        float gridSize = 1000.0f;

        GameObject fieldObject = null;
        GameObject FieldObject
        {
            get
            {
                if (fieldObject == null)
                {
                    var settings = FieldDictionary.Get(0);
                    if (settings != null)
                        fieldObject = Instantiate(settings.FieldObject);
                }

                return fieldObject;
            }
        }

        StaticBulletReceiver staticReceiver = null;
        StaticBulletReceiver StaticReceiver
        {
            get
            {
                if (staticReceiver == null)
                {
                    staticReceiver = this.FieldObject.GetComponent<StaticBulletReceiver>();
                }
                return staticReceiver;
            }
        }

        FieldRealizer fieldRealizer = null;
        FieldRealizer FieldRealizer
        {
            get
            {
                if (fieldRealizer == null)
                {
                    fieldRealizer = this.FieldObject.GetComponent<FieldRealizer>();
                }
                return fieldRealizer;
            }
        }

        public void Setup(World world, Vector3 origin)
        {
            this.world = world;
            this.Origin = origin;
        }

        public void RealizeField(List<TerrainPointInfo> terrainPoints, Coordinates coords, Vector3? center = null)
        {
            this.StaticReceiver.SetWorld(world);
            var pos = center != null ? center.Value: this.Origin;
            this.FieldRealizer.Realize(terrainPoints, coords.ToUnityVector() + this.Origin, pos);
        }
    }
}

