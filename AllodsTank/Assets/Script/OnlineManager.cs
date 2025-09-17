using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OnlineManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_InputField _create;
    [SerializeField] private TMP_InputField _join;
    [SerializeField] private TMP_InputField _nickName;

    [SerializeField] private Button[] _button;
    [SerializeField] internal MountName _name;

    [Header("Team Selection")]
    [SerializeField] private Button[] _buttonTeam;
    [SerializeField] internal teamSelect team;

    private void Start()
    {

        foreach (Button btn in _button)
        {
            string tag = btn.gameObject.tag; // Сохраняем значение
            btn.onClick.AddListener(() => GetMount(tag)); // Передаем сохраненное значение
        }

        foreach (Button btn in _buttonTeam)
        {
            string tag = btn.gameObject.tag; // Сохраняем значение
            btn.onClick.AddListener(() => SelectTeam(tag)); // Передаем сохраненное значение
        }

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "eu"; // Устанавливаем регион
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("Подключение к Photon...");
        }
    }

    public void GetMount(string mountTag)
    {
        _name._mountName = mountTag;
        Debug.Log($"Выбранный mount name: {_name._mountName}");
    }

    // Выбор команды
    public void SelectTeam(string teamTag)
    {
        team.selTeam = teamTag;
    }

    public void CreateRoom()
    {
        if (!CheckNetworkAndNickname()) return;

        if (string.IsNullOrWhiteSpace(_create.text))
        {
            Debug.LogError("Название комнаты не может быть пустым.");
            return;
        }

        // Создаем комнату для игры 5x5
        RoomOptions roomOptions = new()
        {
            MaxPlayers = 10, // 5 игроков в каждой команде
            IsVisible = true,
            IsOpen = true,
            PublishUserId = true
        };

        PhotonNetwork.CreateRoom(_create.text, roomOptions);
        Debug.Log("Запрос на создание комнаты отправлен.");
    }

    public void JoinRoom()
    {
        if (!CheckNetworkAndNickname()) return;

        if (string.IsNullOrWhiteSpace(_join.text))
        {
            Debug.LogError("Название комнаты не может быть пустым.");
            return;
        }

        PhotonNetwork.JoinRoom(_join.text);
        Debug.Log("Запрос на вход в комнату отправлен.");
    }

    private bool CheckNetworkAndNickname()
    {
        if (string.IsNullOrWhiteSpace(_nickName.text))
        {
            Debug.LogError("Поле ника пустое! Введите ник.");
            return false;
        }

        PhotonNetwork.NickName = _nickName.text;

        if (string.IsNullOrEmpty(_name._mountName))
        {
            _name._mountName = "Egida"; // Значение по умолчанию
        }

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.ConnectUsingSettings(); // Переподключение
            return false;
        }

        return true;
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Успешное подключение к мастер-серверу. Входим в лобби...");

        // Настройки сетевой синхронизации применяем здесь
        PhotonNetwork.SendRate = 30;
        PhotonNetwork.SerializationRate = 30;

        PhotonNetwork.JoinLobby();

    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("Game");
    }
}