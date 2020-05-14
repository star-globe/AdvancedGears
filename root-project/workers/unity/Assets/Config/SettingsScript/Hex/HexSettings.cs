using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Field Config/Field Settings", order = 1)]
    public class FieldSettings : ScriptableObject
    {
        [SerializeField] private int id;
        [SerializeField] private float fieldSize;
        [SerializeField] private float updateInterval;

        public int Id => id;
        public float FieldSize => fieldSize;
        public float UpdateInterval => updateInterval;
    }
}
