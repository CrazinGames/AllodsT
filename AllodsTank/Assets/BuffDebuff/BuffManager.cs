using UnityEngine;

public class BuffManager : MonoBehaviour
{
    [SerializeField] private StatsMount statMount; // —сылка на ScriptableObject StatsMount

    public void ApplyBuff(Buff buff)
    {
        if (buff == null || buff.script == null)
        {
            return;
        }

        //  ласс из нашего бафа
        var scriptType = buff.script.GetClass();
        if (scriptType == null)
        {
            return;
        }

        // —оздаЄм экземпл€р класса дл€ передачи параметров
        var buffInstance = System.Activator.CreateInstance(scriptType) as IBuff;
        if (buffInstance == null)
        {
            return;
        }

        // ѕихаем баф в статы нашего объекта
        buffInstance.Apply(statMount);
    }
}

// »змен€ем интерфейс дл€ работы только со StatsMount
public interface IBuff
{
    void Apply(StatsMount targetStats);
}