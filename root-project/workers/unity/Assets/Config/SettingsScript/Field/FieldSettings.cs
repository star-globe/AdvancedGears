using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Field Config/Field Settings", order = 1)]
    public class FieldSettings : ScriptableObject
    {
        [SerializeField] private GameObject fieldObject;
        [SerializeField] private FieldWorkerType workerType;
        [SerializeField] private float fieldSize;
        [SerializeField] private float updateInterval;

        public GameObject FieldObject => fieldObject;
        public FieldWorkerType FieldWorkerType => workerType;
        public float FieldSize => fieldSize;
        public float UpdateInterval => updateInterval;
    }
}
