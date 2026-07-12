using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

public class PopUpGameText : MonoBehaviour
{
    [SerializeField] private TextMeshPro m_text;

    [SerializeField] private AnimationCurve m_curve;
    [SerializeField][Min(0)] private float m_animationTime = 0.5f;
    [SerializeField][Min(0)] private float m_raiseHeight = 1f;
    [SerializeField][Min(0)] private float m_displaceDistance = 1f;
    [SerializeField][Min(0)] private float m_angleSpread = 45f;

    private Transform m_transform;
    private Vector3 m_startPos;

    private IObjectPool<PopUpGameText> m_pool;
    public IObjectPool<PopUpGameText> Pool { private get { return m_pool; } set { m_pool = value; } }

    public string text { set { m_text.text = value; } }

    private void Awake()
    {
        m_transform = transform;
    }

    private void OnDisable()
    {
        if (m_pool != null)
        {
            m_pool.Release(this);
        }
    }

    private IEnumerator RaiseRoutine()
    {

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / m_animationTime;

            m_transform.localScale = Vector3.one * m_curve.Evaluate(t);
            m_transform.position = m_startPos + (Vector3.up * m_raiseHeight + Vector3.forward * m_displaceDistance) * m_curve.Evaluate(t);

            yield return null;
        }


        gameObject.SetActive(false);
    }

    public void OnEmmit(Vector3 point, Vector3 normal)
    {
        m_transform.position = point;
        m_startPos = point; 
        m_transform.rotation = Quaternion.Euler(0, (Random.value - 0.5f) * m_angleSpread, 0);
        m_transform.up = (Camera.main.transform.position - point);

        StartCoroutine(RaiseRoutine());
    }
}
