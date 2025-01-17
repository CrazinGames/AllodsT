using UnityEngine;

public class BuffManager : MonoBehaviour
{
    [SerializeField] private StatsMount statMount; // Ссылка на ScriptableObject StatsMount

    public void ApplyBuff(Buff buff)
    {
        if (buff == null || buff.script == null)
        {
            return;
        }

        // Получаем тип класса из MonoScript
        var scriptType = buff.script.GetClass();
        if (scriptType == null)
        {
            return;
        }

        // Создаём экземпляр класса с передачей параметров
        var buffInstance = System.Activator.CreateInstance(scriptType) as IBuff;
        if (buffInstance == null)
        {
            return;
        }

        // Применяем бафф напрямую к StatsMount
        buffInstance.Apply(statMount);
    }
}

// Изменяем интерфейс для работы только со StatsMount
public interface IBuff
{
    void Apply(StatsMount targetStats);
}