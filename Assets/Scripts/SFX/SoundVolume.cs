using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SoundVolume : MonoBehaviour
{
    [SerializeField] private VolumeAsset m_volumeAsset;
    [SerializeField] private Image m_image;
    [SerializeField] private List<Sprite> m_sprites = new List<Sprite>();

    private Slider m_slider;

    private void Awake()
    {
        m_slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        m_slider.value = m_volumeAsset.Value;
    }

    public void SetValue(float value)
    {
        m_volumeAsset.Value = value;

        int index = Mathf.Clamp(Mathf.CeilToInt(m_sprites.Count * value), 0, m_sprites.Count - 1);

        m_image.sprite = m_sprites[index];
    }
}
