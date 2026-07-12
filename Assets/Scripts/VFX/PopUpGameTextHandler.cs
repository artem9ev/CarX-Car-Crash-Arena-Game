using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PopUpGameTextHandler : MonoBehaviour
{
    [SerializeField] private string m_baseText;
    [SerializeField] private PopUpGameText m_prefab;

    private IObjectPool<PopUpGameText> m_pool;
    private Transform m_transform;

    private void Awake()
    {
        m_transform = transform;
        m_pool = new ObjectPool<PopUpGameText>(Create, Get, Release, Destroy, true, 20, 40);

        List<PopUpGameText> popups = new List<PopUpGameText>();

        for (int i = 0; i < 15; i++)
        {
            popups.Add(m_pool.Get());
        }

        for (int i = popups.Count - 1; i >= 0; i--)
        {
            popups[i].gameObject.SetActive(false);
        }
        popups.Clear();
    }

    private PopUpGameText Create()
    {
        PopUpGameText popup = Instantiate(m_prefab, m_transform);
        popup.Pool = m_pool;
        return popup;
    }

    private void Get(PopUpGameText popup)
    {
        popup.gameObject.SetActive(true);
    }

    private void Release(PopUpGameText effect)
    {
        effect.gameObject.SetActive(false);
    }

    private void Destroy(PopUpGameText popup)
    {
        if (popup != null)
        {
            Destroy(popup.gameObject);
        }
    }

    public void OnEmmit(Vector3 point, string value)
    {
        PopUpGameText popup = m_pool.Get();

        popup.text = m_baseText + value;

        popup.OnEmmit(point, Vector3.up);
    }
}
