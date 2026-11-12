using TapInstinct.Core;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }


    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject levelSelect;
    [SerializeField] private GameObject chatView;

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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
                ShowMainMenu();
                break;
            case GameState.LevelSelection:
                ShowLevelSelect();
                break;
            case GameState.Training:
            case GameState.PlayingLevel:
                ShowChatView();
                break;
        }
    }

    private void ShowMainMenu()
    {
        mainMenu.SetActive(true);
        levelSelect.SetActive(false);
        chatView.SetActive(false);
    }

    private void ShowLevelSelect()
    {
        mainMenu.SetActive(false);
        levelSelect.SetActive(true);
        chatView.SetActive(false);
    }

    private void ShowChatView()
    {
        mainMenu.SetActive(false);
        levelSelect.SetActive(false);
        chatView.SetActive(true);
    }
}
