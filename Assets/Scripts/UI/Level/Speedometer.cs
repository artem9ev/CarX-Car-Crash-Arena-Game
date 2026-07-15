using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Speedometer : MonoBehaviour
{
    [SerializeField] private Image m_segmentPrefab;
    [SerializeField] private RectTransform m_container;
    [Header("UI settings")]
    [SerializeField] private RectTransform m_arrow;
    [SerializeField] private TextMeshProUGUI m_textMaxRPM;
    [SerializeField] private Image m_redline;
    [SerializeField] private Image m_yellowline;
    [SerializeField] private Image m_rightShade;
    [SerializeField] private Image m_leftShade;
    [SerializeField] private float m_angle = 200;
    [SerializeField] private float m_size = 100;
    [SerializeField] private int m_step = 100;

    private int m_maxValue;

    private Transform m_transform;
    private List<Image> m_segments = new();

    private void Awake()
    {
        m_transform = transform;
    }

    public void Initialize(float maxValue)
    {
        int segmentsCount = (int)maxValue / m_step;
        if ((int)maxValue % m_step > m_step * 0.5)
        {
            segmentsCount += 2;
        }
        else if ((int)maxValue % m_step > 0 || (int)maxValue % m_step < m_step * 0.1f)
        {
            segmentsCount++;
        }
        float ang;
        m_maxValue = segmentsCount * m_step;
        m_textMaxRPM.text = m_maxValue.ToString();
        for (int i = 0; i <= segmentsCount; i++) 
        {
            Image segment = Instantiate(m_segmentPrefab, m_container);
            m_segments.Add(segment);

            ang = m_angle * i / segmentsCount - (m_angle - 180) / 2;
            float posX = Mathf.Cos(ang * Mathf.Deg2Rad) * m_size;
            float posY = Mathf.Sin(ang * Mathf.Deg2Rad) * m_size;

            segment.rectTransform.localPosition = new Vector3(posX, posY, 0);
            segment.rectTransform.localRotation = Quaternion.Euler(0, 0, ang - 90);
        }

        m_rightShade.rectTransform.position = m_segments[0].rectTransform.position;
        m_rightShade.rectTransform.localRotation = Quaternion.Euler(0, 0, -(m_angle - 180) / 2);
        m_leftShade.rectTransform.position = m_segments[m_segments.Count - 1].rectTransform.position;
        m_leftShade.rectTransform.localRotation = Quaternion.Euler(0, 0, m_angle - (m_angle - 180) / 2 + 180);

        ang = m_angle * 1f / segmentsCount;

        float offset = 1f * m_angle / 360f;

        m_redline.rectTransform.localRotation = Quaternion.Euler(0, 0, -(m_angle - 180) / 2 + ang * (1 - offset * 360f / m_angle) / 2);
        m_redline.fillAmount = ang * offset / m_angle;

        m_yellowline.rectTransform.localRotation = Quaternion.Euler(0, 0, ang - (m_angle - 180) / 2 + ang * (1 - offset * 360f / m_angle) / 2);
        m_yellowline.fillAmount = ang * offset / m_angle;
    }

    public void Deinitialize()
    {
        for (int i = m_segments.Count - 1; i > 0; i--)
        {
            Image segment = m_segments[i];
            m_segments.Remove(segment);
            Destroy(segment.gameObject);
        }
    }

    public void SetValue(float rpm)
    {
        float value = rpm / m_maxValue;
        float ang = m_angle / 2 - value * m_angle;
        m_arrow.localRotation = Quaternion.Euler(0, 0, ang);
    }
}
