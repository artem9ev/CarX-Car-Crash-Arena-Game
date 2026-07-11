using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class SoundEffect : MonoBehaviour
{
    [SerializeField][Range(0, 2)] private float m_pitctMaxRandomOffset;

    [SerializeField] private AudioSource m_audio;

    private Transform m_transform;
    private float m_pitch;

    private IObjectPool<SoundEffect> m_pool;

    public IObjectPool<SoundEffect> Pool { get { return m_pool; } set { m_pool = value; } }

    public Vector3 Position { get { return m_transform.position;} set { m_transform.position = value; } }

    public float Volume { get { return m_audio.volume; } set { m_audio.volume = value; } }

    private void Awake()
    {
        m_transform = transform;
        m_pitch = m_audio.pitch;
    }

    private void OnEnable()
    {
        m_audio.pitch = m_pitch + (Random.value - 0.5f) * m_pitctMaxRandomOffset;

        m_audio.Play();
        StartCoroutine(PlayRoutine());
    }

    private void OnDisable()
    {
        m_audio.Stop();
        StopAllCoroutines();
    }

    private IEnumerator PlayRoutine()
    {
        if (m_audio != null && m_audio.clip != null)
        {
            yield return new WaitForSeconds(m_audio.clip.length / m_audio.pitch);

            m_pool.Release(this);
        }
    }
}
