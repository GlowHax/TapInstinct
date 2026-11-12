using UnityEngine;

namespace TapInstinct.Core
{
    public enum GameState
    {
        MainMenu,
        LevelSelection,
        Training,
        PlayingLevel,
        GameOverOrFinished
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;

        public GameState CurrentState => currentState;
        public event System.Action<GameState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SetState(GameState.MainMenu);
        }

        public void SetState(GameState newState)
        {
            currentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
