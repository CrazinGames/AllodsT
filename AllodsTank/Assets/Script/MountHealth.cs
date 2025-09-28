using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class MountHealth : MonoBehaviourPun
{
    [SerializeField] private StatsMount _baseStats;
    [SerializeField] private Image _hpSlider;
    [SerializeField] private Battleground kill;

    private StatsMount.MountStatsInstance _stats;

    private void Start()
    {
        GameObject bgObject = GameObject.FindGameObjectWithTag("PlayerUI");
        if (bgObject != null)
        {
            kill = bgObject.GetComponent<Battleground>();
        }

        _stats = new StatsMount.MountStatsInstance(_baseStats);

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
        _stats.HP = Mathf.Max(0, _stats.HP - damage);
        UpdateHealthUI();

        if (_stats.HP <= 0)
        {
            kill.TotalKill(+1);
            gameObject.SetActive(false);
        }
    }

    private void UpdateHealthUI()
    {
        if (_hpSlider != null)
        {
            _hpSlider.fillAmount = _stats.HP / _stats.MaxHP;
        }
    }
}