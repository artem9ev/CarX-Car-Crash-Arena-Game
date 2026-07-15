using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SceneLoaderCurtain : MonoBehaviour
{
    [SerializeField] private Slider m_progressSlider;
    [SerializeField] private TextMeshProUGUI m_progressText;

    private static SceneLoaderCurtain m_instance;
    
    public static SceneLoaderCurtain instance => m_instance;

    private void Awake()
    {
        if (m_instance != null)
        {
            Destroy(this);
            return;
        }
        m_instance = this;

        SetActive(false);
    }

    public void SetActive(bool active)
    {
        foreach (Transform item in transform)
        {
            if (item != transform)
            {
                item.gameObject.SetActive(active);
            }
        }
    }

    public void SetProgressValue(float value)
    {
        if (m_progressSlider != null)
            m_progressSlider.value = value;

        if (m_progressText != null)
            m_progressText.text = (value * 90f).ToString("F0") + "%";
    }
}
