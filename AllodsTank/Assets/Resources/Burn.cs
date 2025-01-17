using UnityEngine;

// Пример реализации баффа
public class Burn : IBuff
{
    private float damage = -20f;

    public void Apply(StatsMount targetStats)
    {
        // Напрямую изменяем HP в StatsMount
        targetStats._hp += damage;
    }
}