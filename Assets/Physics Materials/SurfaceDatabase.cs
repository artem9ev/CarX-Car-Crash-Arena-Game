using System.Collections.Generic;
using UnityEngine;

public class SurfaceDatabase : MonoBehaviour
{
    public static SurfaceDatabase Instance { get; private set; }

    [SerializeField] private SurfaceDefinition[] _definitions;
    private Dictionary<PhysicsMaterial, SurfaceDefinition> _map;

    private void Awake()
    {
        Instance = this;
        _map = new Dictionary<PhysicsMaterial, SurfaceDefinition>();
        foreach (var def in _definitions)
            if (def.physicMaterial != null)
                _map[def.physicMaterial] = def;

        DontDestroyOnLoad(gameObject);
    }

    public SurfaceDefinition Get(PhysicsMaterial mat)
    {
        if (mat != null && _map.TryGetValue(mat, out var def))
            return def;
        return null; // fallback-поверхность по умолчанию
    }

    public SurfaceDefinition GetByType(SurfaceType type)
    {
        foreach (var def in _definitions)
        {
            if (def.surfaceType == type)
            {
                return def;
            }
        }

        return null;
    }
}

public enum SurfaceType
{
    None,
    Ground,
    Road,
    Metal
}