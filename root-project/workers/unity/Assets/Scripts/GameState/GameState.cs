using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AdvancedGears
{
    public enum GameState
    {
        None = 0,
        Init,
        StartConnecting,
        CreatePlayer,
        FieldJoined,
    }
}
