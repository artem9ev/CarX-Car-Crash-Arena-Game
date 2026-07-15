using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RaceResultsView : BaseViewUI, IRaceResultsView
{
    [SerializeField] private Button m_buttonTryAgain;
    [SerializeField] private Button m_buttonBackToMenu;
    [Header("Reward Grooup")]
    [SerializeField] private TextMeshProUGUI m_textRewardCoins;
    [Header("Players results")]
    [SerializeField] private RectTransform m_root;
    [SerializeField] private RacePlayerResult m_prefab;
    [Header("Placing Sprites")]
    [SerializeField] private Sprite m_firstPlayer;
    [SerializeField] private Sprite m_secondPlayer;
    [SerializeField] private Sprite m_thirdPlayer;

    public UnityAction onBackToMenu;
    public UnityAction onTryAgain;

    private void OnEnable()
    {
        m_buttonBackToMenu.onClick.AddListener(OnBackToMenu);
        m_buttonTryAgain.onClick.AddListener(OnTryAgain);
    }

    private void OnDisable()
    {
        m_buttonBackToMenu.onClick.RemoveListener(OnBackToMenu);
        m_buttonTryAgain.onClick.RemoveListener(OnTryAgain);
    }

    public void OnCashAdded(int value)
    {
        m_textRewardCoins.text = $"+{value}";
    }

    private void OnBackToMenu()
    {
        onBackToMenu?.Invoke();
    }

    private void OnTryAgain()
    {
        onTryAgain?.Invoke();
    }

    /*public void SetScore(float value)
    {
        if (m_score == null)
        {
            return;
        }
        m_score.AddExtra(value.ToString());
    }

    public void SetTimeResult(float time)
    {
        if (m_timeResult == null)
        {
            return;
        }
        m_timeResult.AddExtra(time.ToString());
    }*/

    public void SetSessionScore()
    {

    }

    public void SetPlayersTimers(List<PlayerResult> playersResults)
    {
        for (int i = 0; i < m_root.childCount; i++)
        {
            Destroy(m_root.GetChild(i).gameObject);
        }

        for (int i = 0; i < playersResults.Count; i++)
        {
            RacePlayerResult racePlayerResult = Instantiate(m_prefab, m_root);
            racePlayerResult.SetPlayerName(playersResults[i].playerName);
            racePlayerResult.SetPlayerTimeScore(playersResults[i].playerTimeScore);
            racePlayerResult.SetIsCurrentPlayer(playersResults[i].isCurrentPlayer);

            if (i == 0)
            {
                racePlayerResult.SetPlacingImage(m_firstPlayer);
            }
            else if (i == 1)
            {
                racePlayerResult.SetPlacingImage(m_secondPlayer);
            }
            else if (i == 2)
            {
                racePlayerResult.SetPlacingImage(m_thirdPlayer);
            }
            else 
            {
                racePlayerResult.SetPlacingImage(null);
            }
        }
    }
}
