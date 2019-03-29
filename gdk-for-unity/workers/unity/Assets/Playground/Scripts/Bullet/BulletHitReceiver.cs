using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

namespace Playground
{
    public class BulletHitReceiver : MonoBehaviour
    {
        const string bulletTag = "Bullet";

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag != bulletTag)
                return;

            var objEntity = other.GetComponent<GameObjectEntity>();
            if (objEntity == null)
                return;

            var entity = objEntity.Entity;
            var manager = objEntity.EntityManager;
            var info = manager.GetComponentData<BulletInfo>(entity);

            // Damage

            // Destroy Bullet
            //manager.DestroyEntity(entity);

            // Destroy or Deactive pooled bullet object.
            info.IsActive = false;
            other.gameObject.SetActive(false);
            manager.SetComponentData(entity, info);
        }
    }
}
