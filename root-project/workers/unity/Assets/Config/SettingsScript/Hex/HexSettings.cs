using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Strategy Config/Hex Settings", order = 1)]
    public class HexSettings : ScriptableObject
    {
        [SerializeField] private int id;
        [SerializeField] private float fieldSize;
        [SerializeField] private float updateInterval;

        public int Id => id;
        public float FieldSize => fieldSize;
        public float UpdateInterval => updateInterval;
    }
}
