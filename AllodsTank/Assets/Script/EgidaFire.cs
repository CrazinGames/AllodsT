using Photon.Pun;
using UnityEngine;

public class EgidaFire : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Collider2D _collision;
    [SerializeField] private StatsMount _baseStats;

    private StatsMount.MountStatsInstance _stats;
    private PhotonView view;

    private void Start()
    {
        _stats = new StatsMount.MountStatsInstance(_baseStats);
        view = GetComponentInParent<PhotonView>();
    }

    private void FireAnim(bool _bool)
    {
        _animator.SetBool("Fire", _bool);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // Только хозяин объекта может инициировать атаку
        if (!view.IsMine)
            return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PhotonView targetView = collision.gameObject.GetComponent<PhotonView>();
            if (targetView != null && targetView.ViewID != view.ViewID) // Не атакуем себя
            {
                // Вызываем RPC на объекте противника
                targetView.RPC("TakeDamage", RpcTarget.All, _stats.Damage);
            }
        }
    }

    public void Fire()
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