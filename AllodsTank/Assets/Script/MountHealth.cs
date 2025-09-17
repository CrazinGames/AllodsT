using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class MountHealth : MonoBehaviourPun
{
    [SerializeField] private StatsMount _baseStats;
    [SerializeField] private Image _hpSlider;
    private StatsMount.MountStatsInstance _instance;

    private void Start()
    {
        _instance = new StatsMount.MountStatsInstance(_baseStats);

        // Если не установлен в инспекторе — найдём его по тегу
        if (_hpSlider == null && photonView.IsMine)
        {
            GameObject hpObj = GameObject.FindWithTag("PlayerUI")?.transform.Find("HPSlider")?.gameObject;

            if (hpObj != null)
            {
                _hpSlider = hpObj.GetComponent<Image>();
            }
        }

        UpdateHealthUI();
    }

    [PunRPC]
    public void TakeDamage(float damage)
    {
        _instance.TakeDamage(damage, gameObject);

        if (photonView.IsMine)
        {
            UpdateHealthUI();
        }
    }

    private void UpdateHealthUI()
    {
        if (_hpSlider != null)
        {
            _hpSlider.fillAmount = _instance.HP / _instance.MaxHP;
        }
    }
}
