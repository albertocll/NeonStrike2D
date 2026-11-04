using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "NeonStrike2D/LevelData")]
public class LevelData : ScriptableObject
{
    public string sceneName;
    public string displayName;
    public int firstWave = 1;
    public int bossWave = 10;
    public GameObject bossPrefab;
}
