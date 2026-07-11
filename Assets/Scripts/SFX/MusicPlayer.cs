using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    [SerializeField] private VolumeAsset m_volumeAsset;
    
    private AudioSource m_audio;

    private float m_volumeSave;

    private void Awake()
    {
        m_audio = GetComponent<AudioSource>();
    
        m_volumeSave = m_audio.volume;
    }

    private void OnEnable()
    {
        m_volumeAsset.ChangeValue += SetVolume;
    }

    private void OnDisable()
    {
        m_volumeAsset.ChangeValue -= SetVolume;
    }

    private void SetVolume(float value)
    {
        m_audio.volume = value * 2 * m_volumeSave;
    }

    public void Play(bool value)
    {
        if (value)
        {
            m_audio.Play();
        }
        else
        {
            m_audio.Stop();
        }
    }
}
