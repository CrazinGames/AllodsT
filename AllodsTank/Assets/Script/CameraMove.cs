using UnityEngine;

public class CameraMove : MonoBehaviour
{
    [SerializeField] private Vector3 _offset;
    [SerializeField] private Transform _target;
    [SerializeField] private float _damping;
    [SerializeField] private float _maxRadius = 5f;
    private Vector3 _velocity = Vector3.zero;


    public void Start() => _target = GameObject.FindGameObjectWithTag("Player")?.transform;


    internal void camMove(bool _followMouse)
    {

        if (Input.GetMouseButton(1))
        {
            _followMouse = !_followMouse;
        }

        if (_followMouse)
        {
            FollowMouseWithLimit();
        }
        else
        {
            FollowTarget();
        }

    }

    private void FollowTarget()
    {
        Vector3 targetPosition = _target.transform.position + _offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, _damping);
    }

    private void FollowMouseWithLimit()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = -Camera.main.transform.position.z;

        Vector3 worldMousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

        Vector3 targetPosition = worldMousePosition + _offset;

        Vector3 direction = targetPosition - _target.position;
        if (direction.magnitude > _maxRadius)
        {
            direction = direction.normalized * _maxRadius;
        }

        targetPosition = _target.position + direction;

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, _damping);
    }
}
