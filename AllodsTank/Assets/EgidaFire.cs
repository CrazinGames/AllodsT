using UnityEngine;

public class EgidaFire : MonoBehaviour
{
    [SerializeField] private Animator _animator;


    private void FixedUpdate()
    {
        Fire();
    }

    private void FireAnim(bool _bool)
    {
        _animator.SetBool("Fire", _bool);

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Работает");

    }

    private void Fire()
    {
        if (Input.GetMouseButton(0))
        {
            FireAnim(true);
        }
        else
        {
            FireAnim(false);
        }
    }
}