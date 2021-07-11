using System;
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

        public event Action<GameState> StateChanged;

        GameState state = GameState.None;
        public GameState State
        {
            get { return state; }
            set
            {
                if (state == value)
                    return;

                state = value;

                if (stateText != null)
                    stateText.SetText(value.ToString());

                SwitchUI(value);

                if (StateChanged != null)
                    StateChanged.Invoke(value);
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
                case GameState.Init:
                case GameState.StartConnecting:
                case GameState.CreatePlayer:
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
            if (UnityClientConnector.Instance == null)
                return;

            var state = UnityClientConnector.Instance.ConnectionState;

            switch (state)
            {
                case ConnectionState.Connecting:
                    this.State = GameState.StartConnecting;
                    break;

                case ConnectionState.ConnectionEstablished:
                    this.State = GameState.CreatePlayer;
                    break;

                case ConnectionState.PlayerCreated:
                    this.State = GameState.FieldJoined;
                    break;
            }
        }
    }
}
