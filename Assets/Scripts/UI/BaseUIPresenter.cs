using UnityEngine;

public abstract class BaseUIPresenter : MonoBehaviour
{
    protected Transform _transform;

    protected void Awake()
    {
        _transform = transform;
    }

    public void Activate()
    {
        for (int i = 1; i < _transform.childCount; i++)
            _transform.GetChild(i).gameObject.SetActive(true);
    }
    public void Deactivate()
    {
        for (int i = 1; i < _transform.childCount; i++)
            _transform.GetChild(i).gameObject.SetActive(false);
    }
}
