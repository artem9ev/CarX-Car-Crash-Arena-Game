using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RacePauseView : BaseViewUI, IRacePauseView
{
    [Header("Buttons")]
    [SerializeField] private Button m_resumeButton;
    [SerializeField] private Button m_backToMenuButton;

    public event UnityAction onResumeButtonClick;
    public event UnityAction onBackToMenu;

    private void OnEnable()
    {
        m_resumeButton.onClick.AddListener(OnResumeClick);
        m_backToMenuButton.onClick.AddListener(OnBackToMenuClick);
    }

    private void OnDisable()
    {
        m_resumeButton.onClick.RemoveListener(OnResumeClick);
        m_backToMenuButton.onClick.RemoveListener(OnBackToMenuClick);
    }

    public void OnResumeClick()
    {
        onResumeButtonClick?.Invoke();
    }

    public void OnBackToMenuClick()
    {
        onBackToMenu?.Invoke();
    }
}
