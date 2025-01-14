using UnityEngine;

public class CameraMove : MonoBehaviour
{

    [SerializeField] internal Vector3 _offset;

    [SerializeField] private Transform _target;
    [SerializeField] private float _damping;

    private Vector3 _velocity = Vector3.zero;

    private void FixedUpdate()
    {
        Vector3 mP = _target.position + _offset;
        transform.position = Vector3.SmoothDamp(transform.position, mP, ref _velocity, _damping);
    }
}
