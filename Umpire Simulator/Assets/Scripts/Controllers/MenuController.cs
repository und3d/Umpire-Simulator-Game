using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField] private List<CanvasGroup> viewList;
    [SerializeField] private CanvasGroup mainMenu;
    [SerializeField] private CanvasGroup modeSelectionMenu;
    [SerializeField] private CanvasGroup levelSelectionMenu;
    [SerializeField] private CanvasGroup settingsMenu;
    [SerializeField] private CanvasGroup controlsMenu;

    [SerializeField] private List<Button> levelSelectButtons;
    
    private void Awake()
    {
        GoToMainMenu();
        LevelLoader.Instance.SetLevelReferences(levelSelectButtons);
        LevelLoader.Instance.LoadLevelMenu();
    }

    private void DisableViews()
    {
        foreach (var view in viewList)
        {
            view.alpha = 0;
            view.interactable = false;
            view.blocksRaycasts = false;
        }
    }
    
    public void GoToMainMenu()
    {
        DisableViews();
        
        mainMenu.alpha = 1;
        mainMenu.interactable = true;
        mainMenu.blocksRaycasts = true;
        
    }
    
    public void GoToSettingsMenu()
    {
        DisableViews();
        
        settingsMenu.alpha = 1;
        settingsMenu.interactable = true;
        settingsMenu.blocksRaycasts = true;
    }

    public void GoToControlsMenu()
    {
        DisableViews();
        
        controlsMenu.alpha = 1;
        controlsMenu.interactable = true;
        controlsMenu.blocksRaycasts = true;
    }

    public void GoToModeSelection()
    {
        DisableViews();
        
        modeSelectionMenu.alpha = 1;
        modeSelectionMenu.interactable = true;
        modeSelectionMenu.blocksRaycasts = true;
    }

    public void GoToLevelSelection()
    {
        DisableViews();
        levelSelectionMenu.alpha = 1;
        levelSelectionMenu.interactable = true;
        levelSelectionMenu.blocksRaycasts = true;
    }

    public void SetLevel(int level)
    {
        LevelLoader.Instance.SetLevel(level);
    }

    public void GoToEndlessMode()
    {
        SceneManager.LoadScene("EndlessMode");
    }

    public void GoToPracticeMode()
    {
        SceneManager.LoadScene("PracticeMode");
    }
    
    public void QuitGame()
    {
        Application.Quit();
        LevelLoader.Instance.Save();
    }
}
