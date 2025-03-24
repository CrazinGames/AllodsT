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

    private void Start()
    {
        if (!CheckFields()) return;

        foreach (Button btn in _button)
        {
            btn.onClick.AddListener(() => GetMount(btn.gameObject.tag));
        }

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("Подключение к Photon...");
        }
    }

    private bool CheckFields()
    {
        if (_create == null || _join == null || _nickName == null)
        {
            Debug.LogError("Не все поля TMP_InputField назначены в инспекторе!");
            return false;
        }

        if (_name == null)
        {
            Debug.LogError("MountName не назначен! Убедитесь, что он установлен в инспекторе.");
            return false;
        }

        if (_button == null || _button.Length == 0)
        {
            Debug.LogError("Массив _button не инициализирован или пуст!");
            return false;
        }

        return true;
    }

    public void GetMount(string mountTag)
    {
        if (_name == null)
        {
            Debug.LogError("MountName не назначен! Невозможно сохранить mount name.");
            return;
        }

        _name._mountName = mountTag;
        Debug.Log($"Выбранный mount name: {_name._mountName}");
    }

    public void CreateRoom()
    {
        if (!CheckNetworkAndNickname()) return;

        if (string.IsNullOrWhiteSpace(_create.text))
        {
            Debug.LogError("Название комнаты не может быть пустым.");
            return;
        }

        PhotonNetwork.CreateRoom(_create.text, new RoomOptions { MaxPlayers = 4 });
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
            Debug.LogError("Подключение к серверу не установлено.");
            return false;
        }

        return true;
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Успешное подключение к мастер-серверу. Входим в лобби...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Успешное подключение к лобби.");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Подключение к комнате успешно. Загружаем сцену Game...");
        PhotonNetwork.LoadLevel("Game");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Не удалось создать комнату — {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Не удалось подключиться к комнате — {message}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"Отключение от сервера: {cause}");
    }
}
