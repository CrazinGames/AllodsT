using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OneUpdate : MonoBehaviour
{
    private readonly List<IUpdatable> updatableScripts = new List<IUpdatable>();
    private bool initialized = false;

    private void Awake()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            UpdateUpdatableScripts();
        }
    }

    // Обновляем список Updatable-скриптов, вызывается по мере необходимости
    private void UpdateUpdatableScripts()
    {
        updatableScripts.Clear();

        var scripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IUpdatable>()
            .Where(script =>
            {
                var photonView = (script as MonoBehaviour)?.GetComponent<PhotonView>();
                return photonView == null || photonView.IsMine;
            });

        updatableScripts.AddRange(scripts);
        initialized = true;

        Debug.Log($"OneUpdate: Найдено {updatableScripts.Count} скриптов для обновления.");
    }
    private void FixedUpdate()
    {
        if (!initialized) return;

        // Создаем копию списка, чтобы избежать ошибок при изменении списка во время цикла
        var scriptsCopy = updatableScripts.ToList();

        foreach (var script in scriptsCopy)
        {
            try
            {
                script.CustomFixedUpdate();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Ошибка в {script.GetType().Name}: {e.Message}\n{e.StackTrace}");
            }
        }
    }

    internal void RegisterUpdatable(IUpdatable script)
    {
        if (!updatableScripts.Contains(script))
        {
            updatableScripts.Add(script);
            Debug.Log($"OneUpdate: {script.GetType().Name} добавлен в список обновлений.");
        }
    }

    internal void UnregisterUpdatable(IUpdatable script)
    {
        if (updatableScripts.Remove(script))
        {
            Debug.Log($"OneUpdate: {script.GetType().Name} удален из списка обновлений.");
        }
    }

    internal interface IUpdatable
    {
        void CustomFixedUpdate();
    }

    // Метод можно вызывать для ручного обновления списка (при смене сцены или инициализации новых объектов)
    public void RefreshUpdatableScripts()
    {
        UpdateUpdatableScripts();
    }
}
