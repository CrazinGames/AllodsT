using Photon.Pun;
using TMPro;
using UnityEngine;

public class PingUI : MonoBehaviour
{
    [SerializeField] private TMP_Text pingText;
    [SerializeField] private float updateInterval = 1.0f; // ���������� ��� � 1 ���
    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            UpdatePing();
        }
    }

    private void UpdatePing()
    {
        if (pingText == null) return;

        int ping = PhotonNetwork.GetPing(); // �������� ����
        pingText.text = $"Ping: {ping} ms";

        // ������ ���� ������ � ����������� �� �����
        if (ping < 50)
            pingText.color = Color.green; // ������� ����
        else if (ping < 100)
            pingText.color = Color.yellow; // ������� ����
        else
            pingText.color = Color.red; // ������ ����
    }
}
