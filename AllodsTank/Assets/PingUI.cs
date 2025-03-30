using Photon.Pun;
using TMPro;
using UnityEngine;

public class PingUI : MonoBehaviour
{
    [SerializeField] private TMP_Text pingText;
    [SerializeField] private float updateInterval = 1.0f; // Обновление раз в 1 сек
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

        int ping = PhotonNetwork.GetPing(); // Получаем пинг
        pingText.text = $"Ping: {ping} ms";

        // Меняем цвет текста в зависимости от пинга
        if (ping < 50)
            pingText.color = Color.green; // Хороший пинг
        else if (ping < 100)
            pingText.color = Color.yellow; // Средний пинг
        else
            pingText.color = Color.red; // Плохой пинг
    }
}
