using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class SoundEffectPool : MonoBehaviour
{
    //[SerializeField] private VolumeAsset m_volumeAsset;
    [SerializeField] private SoundEffect m_prefab;

    private Transform m_transform;
    private IObjectPool<SoundEffect> m_pool;

    /*private float m_volumeSave;
    private float m_volume;*/

    private void Awake()
    {
        m_transform = transform;
        m_pool = new ObjectPool<SoundEffect>(Create, Get, Release, Destroy, true, 10, 30);

        List<SoundEffect> effectes = new List<SoundEffect>();

        for (int i = 0; i < 18; i++)
        {
            effectes.Add(m_pool.Get());
        }

        for (int i = effectes.Count - 1; i >= 0; i--)
        {
            effectes[i].gameObject.SetActive(false);
        }
        effectes.Clear();
    }

    /*private void Start()
    {
        m_volumeSave = m_prefab.Volume;
    }*/

    /*private void OnEnable()
    {
        m_volumeAsset.ChangeValue += SetVolume;
    }

    private void OnDisable()
    {
        m_volumeAsset.ChangeValue -= SetVolume;
    }*/

    /*private void SetVolume(float value)
    {
        m_volume = value * 2 * m_volumeSave;
    }*/

    private SoundEffect Create()
    {
        SoundEffect effect = Instantiate(m_prefab, m_transform);
        //SetVolume(m_volumeAsset.Value);
        effect.Pool = m_pool;
        return effect;
    }

    private void Get(SoundEffect effect)
    {
        //effect.Volume = m_volume;
        effect.gameObject.SetActive(true);
    }

    private void Release(SoundEffect effect)
    {
        effect.gameObject.SetActive(false);
    }

    private void Destroy(SoundEffect effect)
    {
        if (effect != null)
        {
            Destroy(effect.gameObject);
        }
    }

    public void OnPlaySound(Vector3 position)
    {
        SoundEffect effect = m_pool.Get();
        effect.Position = position;
    }
}
