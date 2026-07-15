using UnityEngine;

[CreateAssetMenu(menuName = "Derby/Surface Definition")]
public class SurfaceDefinition : ScriptableObject
{
    public PhysicsMaterial physicMaterial;
    public SurfaceType surfaceType;

    public float damageMultiplier = 1f;
}