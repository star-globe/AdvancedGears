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

        private void Start()
        {
            if (startButton != null)
                startButton.onClick.AddListener(StartConnect);
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
                    UnityClientConnector.Instance?.CreatePlayerRequest();
                    return;
            }
        }
    }
}
