using UnityEngine;
using System;

public class BuffManager : MonoBehaviour
{
    [SerializeField] private StatsMount statMount; // ������ �� ScriptableObject StatsMount

    public void ApplyBuff(Buff buff)
    {
        if (buff == null || string.IsNullOrEmpty(buff.scriptName))
        {
            Debug.LogWarning("Buff or scriptName is null/empty. Skipping.");
            return;
        }

        // ���� ��� ������ ����� ���������
        var scriptType = Type.GetType(buff.scriptName);
        if (scriptType == null)
        {
            Debug.LogError($"Cannot find script type: {buff.scriptName}");
            return;
        }

        // ������ ��������� ������ ����� ���������
        var buffInstance = Activator.CreateInstance(scriptType) as IBuff;
        if (buffInstance == null)
        {
            Debug.LogError($"Script {buff.scriptName} does not implement IBuff interface.");
            return;
        }

        // ��������� ��� � statMount
        buffInstance.Apply(statMount);
        Debug.Log($"Buff {buff._name} applied successfully.");
    }
}

// ��������� ��� ������ ������ �� StatsMount
public interface IBuff
{
    void Apply(StatsMount targetStats);
}
