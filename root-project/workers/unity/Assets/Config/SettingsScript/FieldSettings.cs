using Improbable.Gdk.ObjectPooling;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Field Config/Field Settings", order = 1)]
    public class FieldSettings : ScriptableObject
    {
        [SerializeField] private GameObject fieldObject;

        public GameObject FieldObject => fieldObject;
    }
}
