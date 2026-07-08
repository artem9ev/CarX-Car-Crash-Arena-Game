using System.Collections.Generic;
using UnityEngine;

public class SpawnPoints : MonoBehaviour
{
    [SerializeField] private List<SpawnPoint> _spawnPointns = new List<SpawnPoint>();

    public int Count => _spawnPointns.Count;

    public SpawnPoint this[int index]
    {
        get
        {
            return _spawnPointns[index];
        }
    }
}
