using System.Collections;
using System.Linq;
using UnityEngine;
using Photon.Pun;

public class OneUpdate : MonoBehaviour
{
    private IUpdatable[] updatableScripts;
    private bool needsRefresh = true; // ���� ��� ����������� ����������

    private void Start()
    {
        StartCoroutine(RefreshRoutine()); // ��������� ���������� �� �������
    }

    private IEnumerator RefreshRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f); // ��������� ��� � 2 �������
            if (needsRefresh)
            {
                UpdateUpdatableScripts();
                needsRefresh = false; // ���������� ���� ����� ����������
            }
        }
    }

    private void UpdateUpdatableScripts()
    {
        // ���������� �������� � ������ Photon
        updatableScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IUpdatable>()
            .Where(script =>
            {
                var photonView = (script as MonoBehaviour)?.GetComponent<PhotonView>();

                // ���� ��� PhotonView, ���������
                if (photonView == null) return true;

                // ���������� ������ �������, ������������� ���������� ������
                return photonView.IsMine;
            })
            .ToArray();

        Debug.Log($"OneUpdate: ������� {updatableScripts.Length} �������� ��� ����������.");
    }

    private void FixedUpdate()
    {
        if (updatableScripts == null || updatableScripts.Length == 0)
        {
            needsRefresh = true; // ������ ����������� ���������� � ������ ����
            return;
        }

        // ���������� ����� CustomFixedUpdate
        foreach (var script in updatableScripts)
        {
            try
            {
                script.CustomFixedUpdate();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"������ � CustomFixedUpdate ��� {script.GetType().Name}: {e}");
            }
        }
    }

    public void RefreshUpdatableScripts()
    {
        needsRefresh = true; // ���� �� ����������
    }

    internal interface IUpdatable
    {
        void CustomFixedUpdate();
    }
}
