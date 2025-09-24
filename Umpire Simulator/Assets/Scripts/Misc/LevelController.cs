using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelController : MonoBehaviour
{
    [SerializeField] LevelDatabase levelDatabase;
    
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
