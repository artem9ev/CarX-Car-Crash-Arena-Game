using UnityEngine;
using UnityEngine.Events;

public abstract class BaseState
{
    public event UnityAction onStart;
    public event UnityAction onUpdate;
    public event UnityAction onEnd;

    public virtual void Start()
    {
        Debug.Log($"[{GetType().ToString()}] - START");
        onStart?.Invoke();
    }
    public virtual void Update()
    {
        //Debug.Log($"[{GetType().ToString()}] - UPDATE");
        onUpdate?.Invoke();
    }
    public virtual void End()
    {
        Debug.Log($"[{GetType().ToString()}] - END");
        onEnd?.Invoke();
    }
}
