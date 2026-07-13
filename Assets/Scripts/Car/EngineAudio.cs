using System.Collections.Generic;
using UnityEngine;

public class EngineAudio : MonoBehaviour
{
    [Header("Engine")]
    [SerializeField] private List<EngineAudioSample> m_audioSamples;
    /*[Header("Transmission")]
    [SerializeField] private AudioSource m_transmissionSource;
    [SerializeField] private float m_transBaseRPM = 2000f;
    [SerializeField] private float m_transVolume = 1f;*/
    [Header("REV")]
    [SerializeField] private AudioSource m_limiterSource;
    [SerializeField] private float m_limiterStepRPM = 300f;
    [SerializeField] private float m_limiterVolume = 1f;

    private CarEngine m_engine;

    private void Awake()
    {
        m_engine = GetComponentInParent<CarEngine>();

        for (int i = 0; i < m_audioSamples.Count; i++)
        {
            m_audioSamples[i].soundOn.volume = 0f;
            m_audioSamples[i].soundOff.volume = 0f;
        }
        //m_transmissionSource.volume = 0f;
        m_limiterSource.volume = 0f;
    }

    private void Update()
    {
        if (m_engine == null)
        {
            return;
        }

        int currentAudioSamplePair = 0;
        for (int i = m_audioSamples.Count - 1; i >= 0; i--)
        {
            if (m_audioSamples[i].baseRPM < m_engine.rpm)
            {
                currentAudioSamplePair = i + 1;
                break;
            }
        }

        if (currentAudioSamplePair == 0)
        {
            SetUpSample(0);
            //Debug.Log($"1");
            for (int i = 1; i < m_audioSamples.Count; i++)
            {
                m_audioSamples[i].soundOn.volume = 0f;
                m_audioSamples[i].soundOff.volume = 0f;
            }
        }
        else if (currentAudioSamplePair == m_audioSamples.Count)
        {
            SetUpSample(m_audioSamples.Count - 1);
            //Debug.Log($"2");
            for (int i = m_audioSamples.Count - 2; i >= 0; i--)
            {
                m_audioSamples[i].soundOn.volume = 0f;
                m_audioSamples[i].soundOff.volume = 0f;
            }
        }
        else
        {
            float volumeModifier = (m_engine.rpm - m_audioSamples[currentAudioSamplePair - 1].baseRPM) / (m_audioSamples[currentAudioSamplePair].baseRPM - m_audioSamples[currentAudioSamplePair - 1].baseRPM);
            //Debug.Log($"3: {currentAudioSamplePair} | {volumeModifier}");
            SetUpSample(currentAudioSamplePair - 1, 1 - volumeModifier);
            SetUpSample(currentAudioSamplePair, volumeModifier);
        }

        /*if (!float.IsInfinity(m_engine.transRPM) && !float.IsNaN(m_engine.transRPM))
        {
            m_transmissionSource.pitch = Mathf.Clamp(m_engine.transRPM / m_transBaseRPM, 0, 20f);
            m_transmissionSource.volume = m_transVolume * Mathf.Clamp(m_engine.transRPM / m_transBaseRPM, 0.2f, 2f);
        }*/

        if (m_engine.rpm > m_engine.peakRPM)
        {
            m_limiterSource.volume = m_limiterVolume * (m_engine.rpm - m_engine.peakRPM) / m_limiterStepRPM;
        }
        else 
        {
            m_limiterSource.volume = 0f;
        }
    }

    private void SetUpSample(int index, float volumeModifier = 1f)
    {
        m_audioSamples[index].SetPitch(m_engine.rpm / m_audioSamples[index].baseRPM);
        m_audioSamples[index].soundOn.volume = m_audioSamples[index].volume * m_engine.gas * volumeModifier;
        m_audioSamples[index].soundOff.volume = m_audioSamples[index].volume * (1 - m_engine.gas) * volumeModifier;
    }

    [System.Serializable]
    private class EngineAudioSample
    {
        [SerializeField] private AudioSource m_soundOn;
        [SerializeField] private AudioSource m_soundOff;
        [SerializeField] private float m_volume = 1f;
        [SerializeField] private float m_baseRpm;
        [SerializeField, Range(0, 5)] private float m_basePitch;

        public AudioSource soundOn => m_soundOn;
        public AudioSource soundOff => m_soundOff;
        public float volume => m_volume;
        public float baseRPM => m_baseRpm;
        public float basePitch => m_basePitch;

        public void SetPitch(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return;
            }
            m_soundOn.pitch = value * basePitch;
            m_soundOff.pitch = value * basePitch;
        }
    }
}
