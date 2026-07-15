using System.Collections.Generic;
using UnityEngine;

public class WheelEffector : MonoBehaviour
{
    [Header("Effects")]
    [SerializeField] private ParticleSystem _dustParticles;
}

[System.Serializable]
public class WheelEffect
{
    [SerializeField] private ParticleSystem _particles;
    [SerializeField] private AudioSource _audio;
}