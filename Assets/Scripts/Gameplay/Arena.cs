using UnityEngine;

public class Arena : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private SpawnPoints _spawnPoints;
    [SerializeField] private float _spawnTime;


    private static Arena _instance;

    public static Arena Instance => _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

    }

    private void Start()
    {
    }
}
