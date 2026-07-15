using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct PlayerResult
{
    public readonly string playerName;
    public readonly float playerTimeScore;
    public readonly bool isCurrentPlayer;

    public PlayerResult(string playerName, string playerImageUrl, float playerTimeScore, bool isCurrentPlayer = false)
    {
        this.playerName = playerName;
        this.playerTimeScore = playerTimeScore;
        this.isCurrentPlayer = isCurrentPlayer;
    }
}

public class RacePlayerResult : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_playerName;
    [SerializeField] private TextMeshProUGUI m_timeScore;
    [SerializeField] private Image m_placingImage;
    [SerializeField] private RectTransform m_activateOnCurrentPlayer;

    public void SetPlayerName(string name)
    {
        m_playerName.text = name;
    }

    public void SetPlayerTimeScore(float time)
    {
        m_timeScore.text = TimeConverter.GetTimer(time);
    }

    public void SetIsCurrentPlayer(bool isCurrent)
    {
        m_activateOnCurrentPlayer.gameObject.SetActive(isCurrent);
    }

    public void SetPlacingImage(Sprite sprite)
    {
        m_placingImage.sprite = sprite;
        m_placingImage.enabled = sprite != null;
    }
}
