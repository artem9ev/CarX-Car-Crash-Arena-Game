using UnityEngine;

public abstract class BaseViewUI : MonoBehaviour
{
    protected Transform _transform;

    protected void Awake()
    {
        _transform = transform;
    }

    public void Activate()
    {
        gameObject.SetActive(true);
    }
    public void Deactivate()
    {
        gameObject.SetActive(false);
    }
}
