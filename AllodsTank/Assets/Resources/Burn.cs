using UnityEngine;

// ������ ���������� �����
public class Burn : IBuff
{
    private float damage = -20f;

    public void Apply(StatsMount targetStats)
    {
        // �������� �������� HP � StatsMount
        targetStats._hp += damage;
    }
}