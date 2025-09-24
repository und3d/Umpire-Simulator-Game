using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct LevelParams
{
    public int level;
    public int pitchAmount;
    public int correctForOneStar;
    public int correctForTwoStars;
    public int correctForThreeStars;
}

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "Scriptable Objects/LevelDatabase")]
public class LevelDatabase : ScriptableObject
{
    public List<LevelParams> levelContainer = new List<LevelParams>();
}
