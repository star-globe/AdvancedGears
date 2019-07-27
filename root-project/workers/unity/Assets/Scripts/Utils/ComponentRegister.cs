using System.Collections;
using System.Collections.Generic;
//using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using UnityEngine;
using Unity.Entities;

public class ComponentRegister : MonoBehaviour
{
    [Require] Entity entity;
    [Require] World world;

    [SerializeField]
    Component[] components;

    void Start()
    {
        foreach(var c in components)
        {
            world.EntityManager.AddComponentObject(entity,c);
        }
    }
}
