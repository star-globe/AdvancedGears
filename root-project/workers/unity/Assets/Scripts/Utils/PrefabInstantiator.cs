using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabInstantiator : MonoBehaviour
{
    [SerializeField]
    GameObject level;

    GameObject levelInstance;

    void Start()
    {
        levelInstance = Instantiate(level, transform.position, transform.rotation);
    }

    void OnDestroy()
    {
        if (levelInstance != null)
        {
            Destroy(levelInstance);
        }
    }
}
