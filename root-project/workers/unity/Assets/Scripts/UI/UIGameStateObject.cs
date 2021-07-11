using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AdvancedGears;

namespace AdvancedGears.UI
{
    public abstract class UIGameStateObject : MonoBehaviour
    {
        [SerializeField]
        GameState[] states;
        public GameState[] States => states;

        public bool ContainsState(GameState state)
        {
            foreach (var s in states) {
                if (s == state)
                    return true;
            }

            return false;
        }
    }
}
