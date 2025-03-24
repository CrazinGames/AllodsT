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

    // ��������� ������ Updatable-��������, ���������� �� ���� �������������
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

        Debug.Log($"OneUpdate: ������� {updatableScripts.Count} �������� ��� ����������.");
    }
    private void FixedUpdate()
    {
        if (!initialized) return;

        // ������� ����� ������, ����� �������� ������ ��� ��������� ������ �� ����� �����
        var scriptsCopy = updatableScripts.ToList();

        foreach (var script in scriptsCopy)
        {
            try
            {
                script.CustomFixedUpdate();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"������ � {script.GetType().Name}: {e.Message}\n{e.StackTrace}");
            }
        }
    }

    internal void RegisterUpdatable(IUpdatable script)
    {
        if (!updatableScripts.Contains(script))
        {
            updatableScripts.Add(script);
            Debug.Log($"OneUpdate: {script.GetType().Name} �������� � ������ ����������.");
        }
    }

    internal void UnregisterUpdatable(IUpdatable script)
    {
        if (updatableScripts.Remove(script))
        {
            Debug.Log($"OneUpdate: {script.GetType().Name} ������ �� ������ ����������.");
        }
    }

    internal interface IUpdatable
    {
        void CustomFixedUpdate();
    }

    // ����� ����� �������� ��� ������� ���������� ������ (��� ����� ����� ��� ������������� ����� ��������)
    public void RefreshUpdatableScripts()
    {
        UpdateUpdatableScripts();
    }
}
