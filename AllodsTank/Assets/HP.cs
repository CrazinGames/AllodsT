using UnityEngine;

public class HP : MonoBehaviour
{
    [SerializeField] private StatsMount stat;
    internal void AddDamage(float damage)
    {
        stat._hp += damage;
        if (stat._hp <= 0)
        {
            stat._hp = 0;
            gameObject.SetActive(false);
            Destroy(gameObject, 500.5f);
        }
    }

}
