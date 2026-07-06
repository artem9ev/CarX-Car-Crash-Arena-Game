using UnityEngine;

public abstract class BaseUIPresenter : MonoBehaviour
{
    public void Activate()
    {
        gameObject.SetActive(true);
    }
    public void Deactivate()
    {
        gameObject.SetActive(false);
    }
}
