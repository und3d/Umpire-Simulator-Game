using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]

public sealed class LevelLoader : MonoBehaviour
{
    [SerializeField] LevelDatabase levelDatabase;
    
    public static LevelLoader Instance { get; private set; }
    public static bool IsAlive => Instance && !_quitting;

    private static bool _quitting;

    private int levelToLoad = -1;
    private int levelPitchAmount = -1;
    private int levelcorrectForOneStar = -1;
    private int levelcorrectForTwoStars = -1;
    private int levelcorrectForThreeStars = -1;

    public int highscore;

    [SerializeField] private List<bool> defaultLevelsUnlocked = new List<bool>(10);
    
    [SerializeField] private List<Button> levels = new List<Button>();
    [SerializeField] private List<bool> levelsUnlocked = new List<bool>();
    [SerializeField] private TMP_Text highscoreText;

    // Clears static state when Play Mode starts with Domain Reload disabled (Editor only)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; _quitting = false; }
    
    // SAVE SECTION
    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    ///  <summary>Writes current game's variables to the save.json</summary>
    public void Save()
    {
        // Load existing or create new
        SaveData data = null;
        if (File.Exists(SavePath))
        {
            try
            {
                var text = File.ReadAllText(SavePath);
                data = JsonUtility.FromJson<SaveData>(text);
            }
            catch
            {
                // Fall through to create new SaveData
            }
        }
        data ??= new SaveData();
        
        // Copy this component's data to the save
        data.levelsUnlockedData = new List<bool>(levelsUnlocked ?? new List<bool>());
        data.highscore = highscore;
        
        // Write to file
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
    }

    /// <summary>Load from save.json. If the file doesn't exist or fails to parse, returns a new SaveData</summary>
    public void Load()
    {
        if (!File.Exists(SavePath))
        {
            levelsUnlocked = defaultLevelsUnlocked;
            highscore = 0;
            return;
        }

        try
        {
            var text = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<SaveData>(text);
            levelsUnlocked = (data?.levelsUnlockedData != null)
                ? new List<bool>(data.levelsUnlockedData)
                : defaultLevelsUnlocked;
            highscore = data?.highscore ?? 0;
        }
        catch
        {
            // Fall through to create new SaveData
            levelsUnlocked = defaultLevelsUnlocked;
        }
    }

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        Load();
    }

    public void LoadLevelMenu()
    {
        var levelIndex = 0;
        foreach (var level in levels)
        {
            level.interactable = levelsUnlocked[levelIndex] switch
            {
                true => true,
                false => false
            };
            levelIndex++;
        }
        highscoreText.text = $"Highscore: {highscore}";
    }

    public void SetLevel(int level, int pitchAmount, int correctForOneStar, int correctForTwoStars,
        int correctForThreeStars)
    {
        levelToLoad = level;
        levelPitchAmount = pitchAmount;
        levelcorrectForOneStar = correctForOneStar;
        levelcorrectForTwoStars = correctForTwoStars;
        levelcorrectForThreeStars = correctForThreeStars;
    }

    public void LoadLevel(out int level, out int pitchAmount, out int correctForOneStar, out int correctForTwoStars, out int correctForThreeStars)
    {
        level = levelToLoad;
        pitchAmount = levelPitchAmount;
        correctForOneStar = levelcorrectForOneStar;
        correctForTwoStars = levelcorrectForTwoStars;
        correctForThreeStars = levelcorrectForThreeStars;
    }
    
    public void UnlockLevel(int level)
    {
        if (level < 0 || level >= levelsUnlocked.Count) return;
        levelsUnlocked[level] = true;
    }

    public void SetLevelReferences(List<Button> levelSelectButtons, TMP_Text highscoreTextMenu)
    {
        levels = levelSelectButtons;
        highscoreText = highscoreTextMenu;
        
    }

    private void OnApplicationQuit()
    {
        Save();
        
        _quitting = true;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Save();
        }
    }

    private void OnDestroy()
    {
        Save();
        
        if (Instance == this) Instance = null;
    }
    
    public void AdvanceLevel(int currentLevel)
    {
        SetLevel(currentLevel + 1);
    }
    
    public void SetLevel(int level)
    {
        var levelParams = levelDatabase.levelContainer[level - 1];
        
        LevelLoader.Instance.SetLevel(
            levelParams.level, 
            levelParams.pitchAmount, 
            levelParams.correctForOneStar, 
            levelParams.correctForTwoStars, 
            levelParams.correctForThreeStars);
        
        SceneManager.LoadScene("LevelMode");
    }
}

public class SaveData
{
    public List<bool> levelsUnlockedData = new List<bool>();
    public int highscore;
}
