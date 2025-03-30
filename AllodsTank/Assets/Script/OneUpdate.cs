using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)] // Запускаем раньше других скриптов
public class OneUpdate : MonoBehaviour
{
    private readonly HashSet<IUpdatable> _updatableScripts = new HashSet<IUpdatable>();
    private bool _isDirty; // Флаг необходимости обновления списка

    private void Awake()
    {
        // Автоматический поиск только при первом запуске
        FindInitialUpdatables();
    }

    private void FixedUpdate()
    {
        if (_isDirty)
        {
            RefreshUpdatableScripts();
            _isDirty = false;
        }

        if (_updatableScripts.Count == 0) return;

        // Используем foreach с копированием для потокобезопасности
        foreach (var script in new List<IUpdatable>(_updatableScripts))
        {
            if (script == null)
            {
                _isDirty = true;
                continue;
            }

            try
            {
                script.CustomFixedUpdate();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in {script.GetType().Name}: {e.Message}\n{e.StackTrace}");
                _isDirty = true;
            }
        }
    }

    private void FindInitialUpdatables()
    {
        _updatableScripts.Clear();

        var allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var script in allScripts)
        {
            if (script is IUpdatable updatable && IsValidForUpdate(script))
            {
                _updatableScripts.Add(updatable);
            }
        }

        Debug.Log($"OneUpdate: Initialized with {_updatableScripts.Count} updatables");
    }

    private bool IsValidForUpdate(MonoBehaviour script)
    {
        if (script == null) return false;

        var photonView = script.GetComponent<PhotonView>();
        return photonView == null || photonView.IsMine;
    }

    public void RegisterUpdatable(IUpdatable script)
    {
        if (script != null && !_updatableScripts.Contains(script))
        {
            _updatableScripts.Add(script);
        }
    }

    public void UnregisterUpdatable(IUpdatable script)
    {
        if (script != null)
        {
            _updatableScripts.Remove(script);
        }
    }

    public void RefreshUpdatableScripts()
    {
        // Очищаем null-ссылки
        _updatableScripts.RemoveWhere(script => script == null);
        _isDirty = false;
    }

    public interface IUpdatable
    {
        void CustomFixedUpdate();
    }
}