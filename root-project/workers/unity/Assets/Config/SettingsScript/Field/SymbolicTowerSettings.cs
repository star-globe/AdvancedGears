using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Field Config/SymbolicTowerSettings", order = 1)]
    class SymbolicTowerSettings : ScriptableObject
    {
        [SerializeField] private GameObject towerObject;
        [SerializeField] private UnitSide side;

        public GameObject TowerObject => towerObject;
        public UnitSide Side => side;
    }
}
