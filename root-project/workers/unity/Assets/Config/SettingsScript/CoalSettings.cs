using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Coal Config/Coal Settings", order = 1)]
    public class CoalSettings : ScriptableObject
    {
        [SerializeField] private GameObject coalModel;

        public GameObject CoalModel => coalModel;

        private void OnValidate()
        {
            if (coalModel != null)
            {
                ValidateGunPrefab(coalModel);
            }
        }

        private void ValidateGunPrefab(GameObject prefab)
        {
        }
    }
}
