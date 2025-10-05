using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EgidaFire : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Collider2D _collision;
    [SerializeField] private StatsMount _baseStats;

    private StatsMount.MountStatsInstance _stats;
    private PhotonView view;

    private float damageInterval = 1.0f; // интервал урона
    private Dictionary<int, Coroutine> activeCoroutines = new();

    private void Start()
    {
        _stats = new StatsMount.MountStatsInstance(_baseStats);
        view = GetComponentInParent<PhotonView>();
    }

    private void FireAnim(bool state)
    {
        _animator.SetBool("Fire", state);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!view.IsMine)
            return;

        if (collision.CompareTag("Player"))
        {
            PhotonView targetView = collision.GetComponent<PhotonView>();
            if (targetView != null && targetView.ViewID != view.ViewID)
            {
                if (!activeCoroutines.ContainsKey(targetView.ViewID))
                {
                    Coroutine routine = StartCoroutine(DealDamageOverTime(targetView));
                    activeCoroutines.Add(targetView.ViewID, routine);
                }
            }
        }
    }

    private IEnumerator DealDamageOverTime(PhotonView targetView)
    {
        while (targetView != null)
        {
            targetView.RPC("TakeDamage", RpcTarget.All, _stats.Damage);
            yield return new WaitForSeconds(damageInterval);
        }
    }

    public void Fire()
    {
        bool isFiring = Input.GetMouseButton(0);

        _collision.enabled = isFiring;
        FireAnim(isFiring);

        if (!isFiring && activeCoroutines.Count > 0)
        {
            foreach (var pair in activeCoroutines)
                StopCoroutine(pair.Value);

            activeCoroutines.Clear();
        }
    }
}
