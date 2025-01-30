using System.Collections;
using System.Linq;
using UnityEngine;
using Photon.Pun;

public class OneUpdate : MonoBehaviour
{
    private IUpdatable[] updatableScripts;
    private bool needsRefresh = true; // Флаг для отложенного обновления

    private void Start()
    {
        StartCoroutine(RefreshRoutine()); // Запускаем обновление по таймеру
    }

    private IEnumerator RefreshRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f); // Проверяем раз в 2 секунды
            if (needsRefresh)
            {
                UpdateUpdatableScripts();
                needsRefresh = false; // Сбрасываем флаг после обновления
            }
        }
    }

    private void UpdateUpdatableScripts()
    {
        // Фильтрация скриптов с учетом Photon
        updatableScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IUpdatable>()
            .Where(script =>
            {
                var photonView = (script as MonoBehaviour)?.GetComponent<PhotonView>();

                // Если нет PhotonView, добавляем
                if (photonView == null) return true;

                // Возвращаем только объекты, принадлежащие локальному игроку
                return photonView.IsMine;
            })
            .ToArray();

        Debug.Log($"OneUpdate: Найдено {updatableScripts.Length} скриптов для обновления.");
    }

    private void FixedUpdate()
    {
        if (updatableScripts == null || updatableScripts.Length == 0)
        {
            needsRefresh = true; // Вместо мгновенного обновления — ставим флаг
            return;
        }

        // Безопасный вызов CustomFixedUpdate
        foreach (var script in updatableScripts)
        {
            try
            {
                script.CustomFixedUpdate();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Ошибка в CustomFixedUpdate для {script.GetType().Name}: {e}");
            }
        }
    }

    public void RefreshUpdatableScripts()
    {
        needsRefresh = true; // Флаг на обновление
    }

    internal interface IUpdatable
    {
        void CustomFixedUpdate();
    }
}
