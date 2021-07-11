using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AdvancedGears;

namespace AdvancedGears.UI
{
    public class StartUI : UIGameStateObject
    {
        [SerializeField]
        Button startButton;

        [SerializeField]
        TextMeshProUGUI buttonText;

        private void Awake()
        {
            MainUI.Instance.StateChanged += SwitchText;
        }

        private void OnDestroy()
        {
            MainUI.Instance.StateChanged -= SwitchText;
        }

        private void Start()
        {
            if (startButton != null)
                startButton.onClick.AddListener(StartConnect);

            SwitchText(MainUI.Instance.State);
        }

        private void SwitchText(GameState state)
        {
            string text = string.Empty;
            bool isEnable = false;
            switch (state)
            {
                case GameState.Init:
                    text = "StartConnect";
                    isEnable = true;
                    break;

                case GameState.StartConnecting:
                    text = "Connecting...";
                    break;

                case GameState.CreatePlayer:
                    text = "FieldJoin";
                    isEnable = true;
                    break;
            }

            if (buttonText != null)
                buttonText.SetText(text);

            if (startButton != null) {
                bool isActive = string.IsNullOrEmpty(text) == false;

                if (startButton.gameObject.activeSelf != isActive)
                    startButton.gameObject.SetActive(isActive);

                startButton.interactable = isEnable;
            }
        }

        private void StartConnect()
        {
            var state = MainUI.Instance.State;

            switch (state)
            {
                case GameState.Init:
                    UnityClientConnector.Instance?.StartConnect();
                    return;

                case GameState.CreatePlayer:
                    UnityClientConnector.Instance?.JoinFieldRequest();
                    return;
            }
        }
    }
}
