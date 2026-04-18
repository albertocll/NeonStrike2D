using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "NeonStrike2D/CharacterData")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public float speed = 8f;
    public int maxHealth = 100;
    public int damage = 1;
    public RuntimeAnimatorController animatorController;
    public GameObject prefab;
}