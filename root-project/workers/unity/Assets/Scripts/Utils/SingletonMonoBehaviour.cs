using System.Collections;
using System.Collections.Generic;
//using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using UnityEngine;
using Unity.Entities;

public class SingletonMonobehaviour<T> : MonoBehaviour where T :MonoBehaviour
{
    public static T Instance { get; private set;}

    void Awake()
    {
        if (Instance == null)
            Instance = this as T;
    }
}
