using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName ="Volume Asset")]
public class VolumeAsset : ScriptableObject
{
    private float m_value = 0.5f;

    public UnityAction<float> ChangeValue;

    public float Value 
    { 
        get
        {
            return m_value;
        }
        set 
        { 
            m_value = value; 
            ChangeValue?.Invoke(m_value);
        }
    }
}
