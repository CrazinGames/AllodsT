using UnityEngine;

public class BuffManager : MonoBehaviour
{
    [SerializeField] private StatsMount statMount; // ������ �� ScriptableObject StatsMount

    public void ApplyBuff(Buff buff)
    {
        if (buff == null || buff.script == null)
        {
            return;
        }

        // ����� �� ������ ����
        var scriptType = buff.script.GetClass();
        if (scriptType == null)
        {
            return;
        }

        // ������ ��������� ������ ��� �������� ����������
        var buffInstance = System.Activator.CreateInstance(scriptType) as IBuff;
        if (buffInstance == null)
        {
            return;
        }

        // ������ ��� � ����� ������ �������
        buffInstance.Apply(statMount);
    }
}

// �������� ��������� ��� ������ ������ �� StatsMount
public interface IBuff
{
    void Apply(StatsMount targetStats);
}