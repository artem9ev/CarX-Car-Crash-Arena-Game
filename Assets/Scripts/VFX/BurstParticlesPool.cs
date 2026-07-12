using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BurstParticlesPool : MonoBehaviour
{
    [SerializeField] private BurstEffect m_prefab;

    private IObjectPool<BurstEffect> m_pool;
    private Transform m_transform;

    private void Awake()
    {
        if (m_prefab == null)
        {
            Debug.LogError("Burst Effect prefab is not Setted!", gameObject);
            throw new System.NullReferenceException();
        }

        m_transform = transform;
        m_pool = new ObjectPool<BurstEffect>(Create, Get, Release, Destroy, true, 20, 40);

        List<BurstEffect> effectes = new List<BurstEffect>();

        for (int i = 0; i < 18; i++)
        {
            effectes.Add(m_pool.Get());
            effectes[i].gameObject.SetActive(false);
        }
        effectes.Clear();
    }

    private BurstEffect Create()
    {
        BurstEffect effect = Instantiate(m_prefab, m_transform);
        effect.gameObject.SetActive(true);
        effect.Pool = m_pool;
        return effect;
    }

    private void Get(BurstEffect effect)
    {
        effect.gameObject.SetActive(true);
    }

    private void Release(BurstEffect effect)
    {
        effect.gameObject.SetActive(false);
    }

    private void Destroy(BurstEffect effect)
    {
        if (effect != null)
        {
            Destroy(effect.gameObject);
        }
    }

    public void OnEmmit(Vector3 position, Vector3 normal)
    {
        if (m_prefab == null)
        {
            Debug.LogError("Burst Effect prefab is not Setted!", gameObject);
            throw new System.NullReferenceException();
        }

        BurstEffect effect = m_pool.Get();

        effect.Emmit(position, Vector3.up);
    }
}