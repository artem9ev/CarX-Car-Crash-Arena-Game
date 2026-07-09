using UnityEngine;

public abstract class BaseState
{
    public virtual void Start()
    {
        Debug.Log($"[{GetType().ToString()}] - START");
    }
    public virtual void Update()
    {
        Debug.Log($"[{GetType().ToString()}] - UPDATE");
    }
    public virtual void End()
    {
        Debug.Log($"[{GetType().ToString()}] - END");
    }
}
