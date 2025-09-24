using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    
    [SerializeField] private List<CanvasGroup> viewList;
    
    [SerializeField] private CanvasGroup gameView;
    [SerializeField] private CanvasGroup pauseMenuView;
    [SerializeField] private CanvasGroup gameButtons;
    [SerializeField] private CanvasGroup levelSelectModeUI;
    [SerializeField] private CanvasGroup practiceModeUI;
    [SerializeField] private CanvasGroup endlessModeUI;
    [SerializeField] private CanvasGroup gameOverView;
    [SerializeField] private TMP_Text correctCallsTextPractice;
    [SerializeField] private TMP_Text correctCallsTextEndless;
    [SerializeField] private TMP_Text remainingPitchesText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text pitchCountText;
    
    [Header("Game Over UI")]
    [SerializeField] private TMP_Text winStatusText;
    [SerializeField] private TMP_Text correctCallsTextGameOver;
    [SerializeField] private Image firstStar;
    [SerializeField] private Image secondStar;
    [SerializeField] private Image thirdStar;
    [SerializeField] private TMP_Text callsNeededForOneStarText;
    [SerializeField] private TMP_Text callsNeededForTwoStarsText;
    [SerializeField] private TMP_Text callsNeededForThreeStarsText;

    [Header("Last Pitch References")] 
    [SerializeField] private Button lastPitchButton;
    [SerializeField] private GameObject strikezoneBox;
    [SerializeField] private TMP_Text continueText;
    
    private CanvasGroup currentView;
    private InputAction pauseAction;
    private InputAction continueAction;

    private bool showingLastPitch;
    private GameObject lastPitch;

    public bool gameButtonsActive;
    public bool showingLevelPitches;
    
    private void Awake()
    {
        DisableViews();
        
        ShowGameView();
        
        pauseAction = InputSystem.actions.FindAction("Pause");
        continueAction = InputSystem.actions.FindAction("Continue");
    }

    private void Update()
    {
        if (gameController.viewingPitch)
            return;
        
        if (pauseAction.WasPressedThisFrame() && currentView == gameOverView && !showingLevelPitches)
        {
            return;
        }
        else if (pauseAction.WasPressedThisFrame() && (currentView != pauseMenuView || showingLastPitch) && !showingLevelPitches)
        {
            ShowPauseMenu();
        }
        else if (pauseAction.WasPressedThisFrame() && currentView == pauseMenuView)
        {
            ShowGameView();
        }
        else if (pauseAction.WasPressedThisFrame() && showingLevelPitches)
        {
            showingLevelPitches = false;
            ShowGameOverView();
        }

        if ((gameController.isEndlessMode || gameController.isPracticeMode) && showingLastPitch && continueAction.WasPressedThisFrame())
        {
            StopAllCoroutines();
            ShowGameView();
        }
    }
    
    public void DisableViews()
    {
        foreach (var view in viewList)
        {
            view.alpha = 0;
            view.interactable = false;
            view.blocksRaycasts = false;
        }
    }

    private void ShowPauseMenu()
    {
        DisableViews();
        if (gameButtonsActive)
            HideGameButtons();
        if (lastPitchButton)
        {
            if (showingLastPitch)
            {
                continueText.enabled = false;
                strikezoneBox.transform.rotation = Quaternion.Euler(0, 180, 90);
                showingLastPitch = false;
                
                Destroy(lastPitch);
            }
            lastPitchButton.interactable = gameController.countdownActive;
            if (gameController.pitchLocationToShow == Vector3.zero)
                lastPitchButton.interactable = false;
        }
        
        pauseMenuView.alpha = 1;
        pauseMenuView.interactable = true;
        pauseMenuView.blocksRaycasts = true;
        Time.timeScale = 0;
        
        currentView = pauseMenuView;
    }
    
    private void ShowGameView()
    {
        DisableViews();
        if (gameButtonsActive)
            ShowGameButtons();
        if (showingLastPitch)
        {
            continueText.enabled = false;
            strikezoneBox.transform.rotation = Quaternion.Euler(0, 180, 90);
            showingLastPitch = false;
            
            Destroy(lastPitch);
        }
        
        gameView.alpha = 1;
        gameView.interactable = true;
        gameView.blocksRaycasts = true;
        Time.timeScale = 1;
        
        currentView = gameView;
    }
    
    private void ShowGameOverView()
    {
        DisableViews();
        
        gameOverView.alpha = 1;
        gameOverView.interactable = true;
        gameOverView.blocksRaycasts = true;
        
        currentView = gameOverView;
    }

    public void ShowPracticeModeUI()
    {
        practiceModeUI.alpha = 1;
        endlessModeUI.alpha = 0;
        levelSelectModeUI.alpha = 0;
    }

    public void ShowEndlessModeUI()
    {
        practiceModeUI.alpha = 0;
        endlessModeUI.alpha = 1;
        levelSelectModeUI.alpha = 0;
    }
    
    public void ShowLevelModeUI()
    {
        practiceModeUI.alpha = 0;
        endlessModeUI.alpha = 0;
        levelSelectModeUI.alpha = 1;
    }

    public void ShowGameButtons()
    {
        gameButtons.alpha = 1;
        gameButtons.interactable = true;
        gameButtons.blocksRaycasts = true;
    }

    public void HideGameButtons()
    {
        gameButtons.alpha = 0;
        gameButtons.interactable = false;
        gameButtons.blocksRaycasts = false;
    }

    public void UpdateCorrectCallsText(int correctCalls)
    {
        correctCallsTextPractice.text = $"Correct: {correctCalls}";
        correctCallsTextEndless.text = $"Correct: {correctCalls}";
        if (correctCallsTextGameOver)
            correctCallsTextGameOver.text = $"Correct Calls: {correctCalls}";
        
    }

    public void UpdateRemainingPitches(int remainingPitches)
    {
        remainingPitchesText.text = $"Remaining: {remainingPitches}";
    }
    
    public void UpdateLivesText(int lives)
    {
        livesText.text = $"Lives: {lives}";
    }
    
    public void UpdatePitchCountText(int pitchCount)
    {
        pitchCountText.text = $"Pitch Count: {pitchCount}";
    }

    public void UpdateWinStatusText(bool winStatus)
    {
        winStatusText.text = winStatus ? "You Win!" : "You Lose!";
    }

    public void UpdateStarScoreText(int oneStar, int twoStars, int threeStars)
    {
        callsNeededForOneStarText.text = $"{oneStar}";
        callsNeededForTwoStarsText.text = $"{twoStars}";
        callsNeededForThreeStarsText.text = $"{threeStars}";
    }

    public void ShowStars(int starCount)
    {
        switch (starCount)
        {
            case 0:
                firstStar.enabled = false;
                secondStar.enabled = false;
                thirdStar.enabled = false;
                break;
            case 1:
                firstStar.enabled = true;
                secondStar.enabled = false;
                thirdStar.enabled = false;
                break;
            case 2:
                firstStar.enabled = true;
                secondStar.enabled = true;
                thirdStar.enabled = false;
                break;
            case 3:
                firstStar.enabled = true;
                secondStar.enabled = true;
                thirdStar.enabled = true;
                break;
        }
    }

    public void GameOver(bool winStatus, int correctCalls, int starsEarned)
    {
        UpdateCorrectCallsText(correctCalls);
        if (winStatusText)
        {
            UpdateWinStatusText(winStatus);
            ShowStars(starsEarned);
        }
        ShowGameOverView();
        Time.timeScale = 0;
    }

    public void ShowLastPitchLocation()
    {
        DisableViews();
        showingLastPitch = true;
        
        continueText.enabled = true;
        strikezoneBox.transform.rotation = Quaternion.Euler(0, 0, 90);
        lastPitch = gameController.ShowLastPitchLocation();
    }

    public void NextLevel()
    {
        LevelLoader.Instance.AdvanceLevel(gameController.level);
    }
    
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    
    public void GoToMainMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}
