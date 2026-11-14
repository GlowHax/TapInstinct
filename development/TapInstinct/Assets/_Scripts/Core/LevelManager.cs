using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Levels (Chats)")]
    [SerializeField] private List<LevelDataSO> levels = new List<LevelDataSO>();
    [SerializeField] private int currentLevelIndex = 0;

    public List<LevelDataSO> Levels => levels;
    public LevelDataSO CurrentLevel => (levels != null && currentLevelIndex >= 0 && currentLevelIndex < levels.Count) ?
        levels[currentLevelIndex] : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool TrySaveHighscore(string levelId, int newAccuracy)
    {
        string key = "Highscore_" + levelId;
        int currentHighscore = PlayerPrefs.GetInt(key, 0);

        if (newAccuracy > currentHighscore)
        {
            PlayerPrefs.SetInt(key, newAccuracy);
            PlayerPrefs.Save();
            return true;
        }

        return false;
    }

    public int GetLevelHighscore(string levelId)
    {
        string key = "Highscore_" + levelId;
        return PlayerPrefs.GetInt(key, 0);


    }

    public bool IsLevelUnlocked(int index)
    {
        if (index == 0) return true;
        if (index < 0 || index >= levels.Count) return false;

        string levelId = levels[index].levelId;
        return PlayerPrefs.GetInt("Unlocked_" + levelId, 0) == 1;
    }

    public void UnlockNextLevel()
    {
        int nextLevelIndex = currentLevelIndex + 1;

        if (nextLevelIndex < levels.Count)
        {
            string nextLevelId = levels[nextLevelIndex].levelId;
            PlayerPrefs.SetInt("Unlocked_" + nextLevelId, 1);
            PlayerPrefs.Save();
            Debug.Log($"[LevelManager] Level {nextLevelId} wurde freigeschaltet!");
        }
    }

    public void SelectLevel(int index)
    {
        if (levels != null && index >= 0 && index < levels.Count)
        {
            currentLevelIndex = index;
        }
    }
}
