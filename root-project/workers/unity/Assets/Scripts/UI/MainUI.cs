using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using TMPro;

namespace AdvancedGears.UI
{
    public class MainUI : SingletonMonobehaviour<MainUI>
    {
        [SerializeField]
        Canvas mainCanvas = null;

        [SerializeField]
        TextMeshProUGUI stateText;

        GameState state = GameState.Init;
        public GameState State
        {
            get { return state; }
            set
            {
                state = value;

                if (stateText != null)
                    stateText.SetText(value.ToString());

                SwitchUI(state);
            }
        }

        private void Awake()
        {
            this.Initialize();
            Assert.IsNotNull(mainCanvas);
        }

        private void Start()
        {
            this.State = GameState.Init;
        }

        private void Update()
        {
            switch (this.State)
            {
                case GameState.StartConnecting:
                    CheckConnection();
                    return;
            }
        }

        UIGameStateObject currentUI = null;

        private void SwitchUI(GameState state)
        {
            if (currentUI != null) {
                if (currentUI.ContainsState(state))
                    return;

                Destroy(currentUI.gameObject);
            }

            var ui = UIGameStateDictionary.GetUIObject(state);
            if (ui != null)
                currentUI = Instantiate(ui, mainCanvas.transform);
        }

        private void CheckConnection()
        {
            if (UnityClientConnector.Instance != null &&
                UnityClientConnector.Instance.IsConnectionEstablished)
                this.State = GameState.CreatePlayer;
        }
    }
}
