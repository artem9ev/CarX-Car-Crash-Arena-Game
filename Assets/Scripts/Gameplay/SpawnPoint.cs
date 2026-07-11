using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private Vector3 _boxSize = new Vector3(5, 5, 5);
    [SerializeField] private LayerMask _layerMask;

    private Transform _transform;

    public bool IsClear => !Physics.CheckBox(_transform.position + _transform.up * _boxSize.y / 2, _boxSize / 2, _transform.rotation, _layerMask);

    private void OnDrawGizmos()
    {
        if (_transform == null)
        {
            _transform = transform;
        }

        Gizmos.color = Color.magenta;

        Vector3[] points =
        {
            _transform.TransformPoint(new Vector3(_boxSize.x / 2, 0, _boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(_boxSize.x / 2, 0, -_boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(-_boxSize.x / 2, 0, _boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(-_boxSize.x / 2, 0, -_boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(_boxSize.x / 2, _boxSize.y, _boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(_boxSize.x / 2, _boxSize.y, -_boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(-_boxSize.x / 2, _boxSize.y, _boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(-_boxSize.x / 2, _boxSize.y, -_boxSize.z / 2)),

            _transform.TransformPoint(new Vector3(_boxSize.x / 2, 0, _boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(-_boxSize.x / 2, 0, _boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(_boxSize.x / 2, 0, -_boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(-_boxSize.x / 2, 0, -_boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(_boxSize.x / 2, _boxSize.y, _boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(-_boxSize.x / 2, _boxSize.y, _boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(_boxSize.x / 2, _boxSize.y, -_boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(-_boxSize.x / 2, _boxSize.y, -_boxSize.z / 2)),

            _transform.TransformPoint(new Vector3(_boxSize.x / 2, 0, _boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(_boxSize.x / 2, _boxSize.y, _boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(-_boxSize.x / 2, 0, _boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(-_boxSize.x / 2, _boxSize.y, _boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(_boxSize.x / 2, 0, -_boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(_boxSize.x / 2, _boxSize.y, -_boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(-_boxSize.x / 2, 0, -_boxSize.z / 2)),
            _transform.TransformPoint(new Vector3(-_boxSize.x / 2, _boxSize.y, -_boxSize.z / 2)),
        };

        Gizmos.DrawLineList(points);

        Gizmos.color = Color.green;
        
        Gizmos.DrawSphere(_transform.position + 0.5f * _transform.up, 0.2f);
        Gizmos.DrawRay(_transform.position + 0.5f * _transform.up, _transform.forward * 2);
    }

    private void Awake()
    {
        _transform = transform;
    }

    public bool TryGetPoint(out Vector3 position, out Quaternion rotation)
    {
        position = _transform.position + 0.5f * _transform.up;
        rotation = _transform.rotation;
        return IsClear;
    }
}
