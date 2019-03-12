using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Entities;
using Improbable.Gdk.GameObjectRepresentation;

namespace Playground
{
    internal class BulletCreator : MonoBehaviour
    {
        EntityManager entityManager;

        [Require] BulletComponent.Requirable.Reader reader;

        [SerializeField]
        GameObject bulletObject;

        private void Start()
        {
            Assert.IsNotNull(bulletObject);
        }

        public void Setup(EntityManager entity)
        {
            entityManager = entity;
            reader.OnFires += OnFire;
        }

        void OnFire(BulletFireInfo info)
        {
            // use Linq

            var obj = Instantiate(bulletObject);

            var entity = obj.GetComponent<GameObjectEntity>();

            entityManager.AddComponentData(entity.Entity, new BulletInfo(info));
        }
    }

}
