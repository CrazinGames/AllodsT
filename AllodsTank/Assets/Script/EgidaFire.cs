using Photon.Pun;
using UnityEngine;

public class EgidaFire : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Collider2D _collision;
    [SerializeField] private StatsMount _stat;
    [SerializeField] private PhotonView view; // ��������� PhotonView

    private void Start()
    {
        view = GetComponentInParent<PhotonView>();
    }

    private void FireAnim(bool _bool)
    {
        _animator.SetBool("Fire", _bool);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!view.IsMine) return; // ������ ������ ������� ����� ���������

        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent<PhotonView>(out var targetView))
            {
                if (!targetView.IsMine) // ����� �� ��������� ����
                {
                    Debug.Log($"������� {collision.gameObject.name}");
                    targetView.RPC("TakeDamage", RpcTarget.All, _stat._damage);
                }
            }
        }
    }

    internal void Fire()
    {
        if (Input.GetMouseButton(0))
        {
            _collision.enabled = true;
            FireAnim(true);
        }
        else
        {
            FireAnim(false);
            _collision.enabled = false;
        }
    }
}
