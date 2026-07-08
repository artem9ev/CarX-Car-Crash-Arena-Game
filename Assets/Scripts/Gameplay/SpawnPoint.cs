using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private Vector3 _boxSize = new Vector3(5, 5, 5);
    [SerializeField] private LayerMask _layerMask;

    private Transform _transform;

    private bool _isClear;

    public bool IsClear => _isClear;

    private void OnDrawGizmos()
    {
        if (_transform == null)
        {
            _transform = transform;
        }

        Gizmos.color = Color.magenta;

        Gizmos.DrawCube(_transform.position + _transform.up * _boxSize.y / 2, _boxSize );
    }

    private void Awake()
    {
        _transform = transform;
    }

    public bool TryGetPoint(out Vector3 position, out Quaternion rotation)
    {
        position = _transform.position;
        rotation = _transform.rotation;
        _isClear = Physics.BoxCast(_transform.position + _transform.up * _boxSize.y / 2, _boxSize, Vector3.zero, out RaycastHit hit, _transform.rotation, 0, _layerMask);
        return _isClear;
    }
}
