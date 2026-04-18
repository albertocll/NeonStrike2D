using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private CharacterData[] characters;

    void Awake()
    {
        string selected = GameData.SelectedCharacter;

        foreach (var data in characters)
        {
            if (data.characterName == selected)
            {
                GameObject player = Instantiate(data.prefab, transform.position, Quaternion.identity);

                var health = player.GetComponent<PlayerHealth>();
                if (health) health.Init(data.maxHealth);

                var controller = player.GetComponent<PlayerController>();
                if (controller) controller.Init(data.speed, data.damage);

                var anim = player.GetComponentInChildren<Animator>();
                if (anim && data.animatorController)
                    anim.runtimeAnimatorController = data.animatorController;

                return;
            }
        }

        Debug.LogError($"[PlayerSpawner] No se encontró CharacterData para: {selected}");
    }
}