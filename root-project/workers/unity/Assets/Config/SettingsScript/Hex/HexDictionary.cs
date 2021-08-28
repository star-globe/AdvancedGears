using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Strategy Config/Hex Dictionary", order = 0)]
    public class HexDictionary : DictionarySettings
    {
        public static HexDictionary Instance { get; private set; }

        [SerializeField] private HexSettings[] hexesList;
        readonly Dictionary<int, HexSettings> hexesDic = new Dictionary<int, HexSettings>();

        [SerializeField] private float edgeLength;
        public float EdgeLength => edgeLength;
        public static float HexEdgeLength => Instance.edgeLength;

        [SerializeField] private float hexTargetRadiusRate = 0.05f;
        public static float HexTargetRadius => HexEdgeLength * Instance.hexTargetRadiusRate;

        [SerializeField] private float hexResourceRate = 10.0f;
        public static float HexResourceRate => Instance.hexResourceRate;

        [SerializeField] private float hexPowerLimit = 100.0f;
        public float PowerLimit => hexPowerLimit;
        public static float HexPowerLimit => Instance.hexPowerLimit;

        [SerializeField] private float hexPowerMin = 0.1f;
        public static float HexPowerMin => Instance.hexPowerMin;

        [SerializeField] private float hexPowerDomination = 50.0f;
        public static float HexPowerDomination => Instance.hexPowerDomination;

        [SerializeField] private int resourceFlowMax = 3;
        public static int ResourceFlowMax => Instance.resourceFlowMax;

        public override void Initialize()
        {
            Instance = this;

            foreach (var h in hexesList)
                hexesDic[h.Id] = h;
        }

        public static HexSettings Get(int hex_type_id)
        {
            if (Instance == null)
            {
                Debug.LogError("The Hex Dictionary has not been set.");
                return null;
            }

            HexSettings settings = null;
            Instance.hexesDic.TryGetValue(hex_type_id, out settings);

            return settings;
        }
    }
}
