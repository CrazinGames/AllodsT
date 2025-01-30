using UnityEngine;
using System;

public class BuffManager : MonoBehaviour
{
    [SerializeField] private StatsMount statMount; // Ссылка на ScriptableObject StatsMount

    public void ApplyBuff(Buff buff)
    {
        if (buff == null || string.IsNullOrEmpty(buff.scriptName))
        {
            Debug.LogWarning("Buff or scriptName is null/empty. Skipping.");
            return;
        }

        // Ищем тип класса через рефлексию
        var scriptType = Type.GetType(buff.scriptName);
        if (scriptType == null)
        {
            Debug.LogError($"Cannot find script type: {buff.scriptName}");
            return;
        }

        // Создаём экземпляр класса через рефлексию
        var buffInstance = Activator.CreateInstance(scriptType) as IBuff;
        if (buffInstance == null)
        {
            Debug.LogError($"Script {buff.scriptName} does not implement IBuff interface.");
            return;
        }

        // Применяем баф к statMount
        buffInstance.Apply(statMount);
        Debug.Log($"Buff {buff._name} applied successfully.");
    }
}

// Интерфейс для работы только со StatsMount
public interface IBuff
{
    void Apply(StatsMount targetStats);
}
