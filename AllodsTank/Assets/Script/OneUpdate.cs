using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;

public class OneUpdate : MonoBehaviour
{
    private List<IUpdatable> updatableScripts = new List<IUpdatable>();

    private void Awake() => UpdateUpdatableScripts();

    private void UpdateUpdatableScripts()
    {
        updatableScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IUpdatable>()
            .Where(script =>
            {
                var photonView = (script as MonoBehaviour)?.GetComponent<PhotonView>();
                return photonView == null || photonView.IsMine;
            })
            .ToList();
    }

    private void FixedUpdate()
    {
        foreach (var script in updatableScripts)
        {
            try
            {
                script.CustomFixedUpdate();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Ошибка в {script.GetType().Name}: {e.Message}\n{e.StackTrace}");
                // Опционально: удалить проблемный скрипт из списка
            }
        }
    }

    internal void RegisterUpdatable(IUpdatable script)
    {
        if (!updatableScripts.Contains(script))
        {
            updatableScripts.Add(script);
        }
    }

    internal void UnregisterUpdatable(IUpdatable script)
    {
        updatableScripts.Remove(script);
    }

    internal interface IUpdatable
    {
        void CustomFixedUpdate();
    }
}