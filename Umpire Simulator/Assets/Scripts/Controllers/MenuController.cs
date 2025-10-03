using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MenuController : MonoBehaviour
{
    [SerializeField] private List<CanvasGroup> viewList;
    [SerializeField] private CanvasGroup mainMenu;
    [SerializeField] private CanvasGroup modeSelectionMenu;
    [SerializeField] private CanvasGroup levelSelectionMenu;
    [SerializeField] private CanvasGroup settingsMenu;
    [SerializeField] private CanvasGroup controlsMenu;
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text creatorText;
    [SerializeField] private CanvasGroup selectedLevelMenu;
    [SerializeField] private TMP_Text selectedLevelText;
    [SerializeField] private TMP_Text pitchCountText;
    [SerializeField] private TMP_Text levelCorrectForOneStarText;
    [SerializeField] private TMP_Text levelCorrectForTwoStarsText;
    [SerializeField] private TMP_Text levelCorrectForThreeStarsText;

    [SerializeField] private List<Button> levelSelectButtons;
    [SerializeField] private TMP_Text highscoreTextMenu;
    [SerializeField] private AudioSource buttonSound;

    [Header("Music")] 
    [SerializeField] private Slider menuMusicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider callsSlider;
    [SerializeField] private AudioSource menuSong;
    [SerializeField] private AudioClip songOne;
    [SerializeField] private AudioClip songTwo;
    [SerializeField] private AudioClip songThree;
    [SerializeField] private AudioClip songFour;
    [SerializeField] private AudioClip songFive;
    [SerializeField] private AudioClip songSix;

    private int selectedLevel;
    private int pitchCount;
    private int levelCorrectForOneStar = -1;
    private int levelCorrectForTwoStars = -1;
    private int levelCorrectForThreeStars = -1;
    
    private void Awake()
    {
        LevelLoader.Instance.versionText = versionText;
        LevelLoader.Instance.SetVersionText();
        GoToMainMenu();
        SetVolumes();
        PlaySong();
        LevelLoader.Instance.SetLevelReferences(levelSelectButtons, highscoreTextMenu, this);
        LevelLoader.Instance.LoadLevelMenu();
    }

    private void DisableViews()
    {
        titleText.enabled = true;
        creatorText.enabled = true;
        
        foreach (var view in viewList)
        {
            view.alpha = 0;
            view.interactable = false;
            view.blocksRaycasts = false;
        }
    }

    public void SetVolumes()
    {
        menuMusicSlider.value = LevelLoader.Instance.musicVolume;
        sfxSlider.value = LevelLoader.Instance.sfxVolume;
        callsSlider.value = LevelLoader.Instance.callsVolume;
        menuSong.volume = LevelLoader.Instance.musicVolume;
        buttonSound.volume = LevelLoader.Instance.sfxVolume;
    }
    
    public void GoToMainMenu()
    {
        buttonSound.Play();
        DisableViews();
        //LevelLoader.Instance.Save();
        
        mainMenu.alpha = 1;
        mainMenu.interactable = true;
        mainMenu.blocksRaycasts = true;
        
    }
    
    public void GoToSettingsMenu()
    {
        buttonSound.Play();
        DisableViews();
        SetVolumes();
        
        settingsMenu.alpha = 1;
        settingsMenu.interactable = true;
        settingsMenu.blocksRaycasts = true;
    }

    public void GoToControlsMenu()
    {
        buttonSound.Play();
        DisableViews();
        
        titleText.enabled = false;
        creatorText.enabled = false;
        controlsMenu.alpha = 1;
        controlsMenu.interactable = true;
        controlsMenu.blocksRaycasts = true;
    }

    public void GoToModeSelection()
    {
        buttonSound.Play();
        DisableViews();
        
        modeSelectionMenu.alpha = 1;
        modeSelectionMenu.interactable = true;
        modeSelectionMenu.blocksRaycasts = true;
    }

    public void GoToLevelSelection()
    {
        buttonSound.Play();
        DisableViews();
        levelSelectionMenu.alpha = 1;
        levelSelectionMenu.interactable = true;
        levelSelectionMenu.blocksRaycasts = true;
    }
    
    public void ShowLevel(int level)
    {
        selectedLevelMenu.alpha = 1;
        selectedLevelMenu.interactable = true;
        selectedLevelMenu.blocksRaycasts = true;

        var levelParams = LevelLoader.Instance.GetLevelValues(level);
        pitchCount = levelParams.pitchAmount;
        levelCorrectForOneStar = levelParams.correctForOneStar;
        levelCorrectForTwoStars = levelParams.correctForTwoStars;
        levelCorrectForThreeStars = levelParams.correctForThreeStars;
        
        selectedLevelText.text = $"Level {level}";
        pitchCountText.text = $"{pitchCount}";
        levelCorrectForOneStarText.text = $"{levelCorrectForOneStar}";
        levelCorrectForTwoStarsText.text = $"{levelCorrectForTwoStars}";
        levelCorrectForThreeStarsText.text = $"{levelCorrectForThreeStars}";
        
        selectedLevel = level;
    }

    private void PlaySong()
    {
        var songID = Random.Range(0, 11);

        menuSong.clip = songID switch
        {
            0 or 1 => songOne,
            2 or 3 => songTwo,
            4 or 5 => songThree,
            6 or 7 => songFour,
            8 or 9 => songFive,
            10 => songSix,
            _ => null
        };

        if (!menuSong.clip)
        {
            Debug.Log($"No song found. Song ID: {songID}. Trying again.");
            PlaySong();
        }
        
        menuSong.Play();
        StartCoroutine(PlaySongCoroutine(menuSong.clip.length));
    }

    private IEnumerator PlaySongCoroutine(float songDuration)
    {
        var elapsed = 0f;
        while (elapsed < songDuration + 15f)
        {
            elapsed += Time.unscaledDeltaTime;
            
            yield return null;
        }
        
        PlaySong();
    }
    
    public void SetMusicVolume()
    {
        LevelLoader.Instance.musicVolume = menuMusicSlider.value;
        menuSong.volume = LevelLoader.Instance.musicVolume;
    }

    public void SetsfxVolume()
    {
        LevelLoader.Instance.sfxVolume = sfxSlider.value;
    }
    
    public void SetCallsVolume()
    {
        LevelLoader.Instance.callsVolume = callsSlider.value;
    }

    public void SetLevel(int level)
    {
        buttonSound.Play();
        ShowLevel(level);
        
    }

    public void PlayLevel()
    {
        LevelLoader.Instance.SetLevel(selectedLevel);
    }

    public void GoToEndlessMode()
    {
        buttonSound.Play();
        SceneManager.LoadScene("EndlessMode");
    }

    public void GoToPracticeMode()
    {
        buttonSound.Play();
        SceneManager.LoadScene("PracticeMode");
    }
    
    public void QuitGame()
    {
        buttonSound.Play();
        Application.Quit();
        LevelLoader.Instance.Save();
    }
}
