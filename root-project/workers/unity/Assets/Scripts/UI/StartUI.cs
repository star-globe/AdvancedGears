using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AdvancedGears.Scripts.UI
{
    public class StartUI : MonoBehaviour
    {
        enum GameState
        {
            Init = 0,
            StartConnect,
            Connected,
            CreatePlayer,
        }

        [SerializeField]
        GameObject uiObject = null;

        [SerializeField]
        Button startButton;

        [SerializeField]
        TextMeshProUGUI stateText;

        GameState state = GameState.Init;
        GameState State
        {
            get { return state; }
            set
            {
                state = value;

                if (stateText != null)
                    stateText.SetText(value.ToString());
            }
        }

        private void Start()
        {
            this.State = GameState.Init;

            if (startButton != null)
                startButton.onClick.AddListener(StartConnect);
        }

        private void Update()
        {
            switch (state)
            {
                case GameState.StartConnect:
                    CheckConnection();
                    break;
            }
        }


        private void StartConnect()
        {
            if (this.State == GameState.Init) {
                this.State = GameState.StartConnect;
                UnityClientConnector.Instance?.StartConnect();
                return;
            }

            if (this.State == GameState.Connected) {
                this.State = GameState.CreatePlayer;
                UnityClientConnector.Instance?.CreatePlayerRequest();
                return;
            }
        }

        private void CheckConnection()
        {
            if (UnityClientConnector.Instance != null &&
                UnityClientConnector.Instance.IsConnectionEstablished)
                this.State = GameState.Connected;
        }
    }
}
