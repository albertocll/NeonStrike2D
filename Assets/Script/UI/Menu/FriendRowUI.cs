using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private Button challengeButton;
    [SerializeField] private Button chatButton;
    [SerializeField] private GameObject unreadIndicator;

    public TMP_Text UsernameText => usernameText;
    public Button ChallengeButton => challengeButton;
    public Button ChatButton => chatButton;

    public string Username { get; private set; }

    public void Setup(string username)
    {
        Username = username;
        usernameText.text = username;
        SetUnread(false);
    }

    public void SetUnread(bool hasUnread)
    {
        if (unreadIndicator != null)
            unreadIndicator.SetActive(hasUnread);
    }
}
