using UnityEngine;

[CreateAssetMenu(menuName = "Derby/Surface Definition")]
public class SurfaceDefinition : ScriptableObject
{
    public PhysicsMaterial physicMaterial;
    public SurfaceType surfaceType;
    public Color dustColor = Color.white;
    public AudioClip[] rollingClips;
    public float dustEmissionMultiplier = 1f;
}