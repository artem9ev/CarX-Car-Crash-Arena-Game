using UnityEngine;
using UnityEngine.Pool;

public class BurstEffect : MonoBehaviour
{
    private ParticleSystem m_particles;
    private Transform m_transform;

    private IObjectPool<BurstEffect> m_pool;
    public IObjectPool<BurstEffect> Pool { private get { return m_pool; } set { m_pool = value; } }

    private void Awake()
    {
        m_transform = transform;
        m_particles = GetComponent<ParticleSystem>();
    }

    private void OnParticleSystemStopped()
    {
        m_pool.Release(this);
    }

    public void Emmit(Vector3 position, Vector3 normal)
    {
        m_transform.position = position;
        m_transform.forward = normal;

        m_particles.Play();
    }
}
