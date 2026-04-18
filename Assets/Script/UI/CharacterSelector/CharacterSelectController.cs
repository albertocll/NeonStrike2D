using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelectController : MonoBehaviour
{
    [Header("Info Panels")]
    public GameObject infoPanel_Violet;
    public GameObject infoPanel_Cyrus;
    public GameObject infoPanel_Nyx;
    public GameObject infoPanel_Atlas;

    public void ClickViolet() => ShowPanel(infoPanel_Violet);
    public void ClickCyrus()  => ShowPanel(infoPanel_Cyrus);
    public void ClickNyx()    => ShowPanel(infoPanel_Nyx);
    public void ClickAtlas()  => ShowPanel(infoPanel_Atlas);

    public void DeployViolet() => Deploy("Violet");
    public void DeployCyrus()  => Deploy("Cyrus");
    public void DeployNyx()    => Deploy("Nyx");
    public void DeployAtlas()  => Deploy("Atlas");

    private void Deploy(string characterName)
    {
        GameData.SelectedCharacter = characterName;
        SceneManager.LoadScene("Level1");
    }

    private void ShowPanel(GameObject target)
    {
        SafeSetActive(infoPanel_Violet, false);
        SafeSetActive(infoPanel_Cyrus,  false);
        SafeSetActive(infoPanel_Nyx,    false);
        SafeSetActive(infoPanel_Atlas,  false);
        SafeSetActive(target, true);
    }

    private void SafeSetActive(GameObject go, bool value)
    {
        if (go != null) go.SetActive(value);
    }
}