using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TapInstinct.Core;

namespace TapInstinct.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Menu Panels")]
        [Tooltip("The main container for the Title screen")]
        [SerializeField] private GameObject menuRoot;
        [Tooltip("Container for Level Selection view")]
        [SerializeField] private GameObject levelSelectPanel;
        [Tooltip("Reference to the Chat UI container to toggle gameplay view")]
        [SerializeField] private GameObject gameplayContainer;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button trainingButton;
        [SerializeField] private Button levelSelectButton;
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            if (trainingButton != null)
                trainingButton.onClick.AddListener(OnTrainingClicked);

            if (levelSelectButton != null)
                levelSelectButton.onClick.AddListener(OnLevelSelectClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
        }

        public void OnTrainingClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Training);
            }
        }

        public void OnLevelSelectClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.LevelSelection);
            }
        }

        public void OnQuitClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.QuitGame();
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }
}