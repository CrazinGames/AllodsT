using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;

public class OnlineManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    [SerializeField] private TMP_InputField _create;
    [SerializeField] private TMP_InputField _join;
    [SerializeField] private TMP_InputField _nickName;

    [SerializeField] private Button[] _button;
    [SerializeField] internal MountName _name;
    
    [Header("Team Selection")]
    [SerializeField] private Button[] _buttonTeam;
    [SerializeField] internal teamSelect team;

    [Header("Network Settings")]
    [SerializeField] private float timeResyncInterval = 30f; // Интервал ресинхронизации времени
    [SerializeField] private int historyBufferSize = 100; // Размер буфера для истории действий
    [SerializeField] private float maxRollbackTime = 1.5f; // Максимальное время отката в секундах
    
    // Константы для сетевых событий
    private const byte EVENT_ROLLBACK = 1;
    private const byte EVENT_TIME_SYNC = 2;
    private const byte EVENT_TEAM_SELECTION = 3;
    
    // Информация о пинге и синхронизации времени
    private float localTimeOffset = 0f; // Разница между локальным и серверным временем
    private double lastTimeSync = 0;
    private List<double> pingMeasurements = new List<double>();
    private int maxPingMeasurements = 10;
    
    // Счетчик для синхронизации времени
    private Coroutine timeResyncCoroutine;
    
    // Структура для хранения информации об откате
    public struct RollbackInfo
    {
        public int playerId;
        public int sequenceNumber;
        public float duration;
        public double timestamp;
    }
    
    // Список активных откатов
    private List<RollbackInfo> activeRollbacks = new List<RollbackInfo>();

    private void Start()
    {
        if (!CheckFields()) return;

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
    
    private void OnDestroy()
    {
        if (timeResyncCoroutine != null)
        {
            StopCoroutine(timeResyncCoroutine);
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
        RoomOptions roomOptions = new RoomOptions 
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
        
        // Запускаем периодическую синхронизацию времени
        if (timeResyncCoroutine != null)
        {
            StopCoroutine(timeResyncCoroutine);
        }
        timeResyncCoroutine = StartCoroutine(ResynchronizeTimeRoutine());
    }
    
    private IEnumerator ResynchronizeTimeRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(timeResyncInterval);
        
        while (true)
        {
            SynchronizeTime();
            yield return wait;
        }
    }
    
    private void SynchronizeTime()
    {
        // Запоминаем локальное время отправки
        double localTimeSent = Time.time;
        
        // Отправляем запрос на синхронизацию времени
        object[] content = new object[] { localTimeSent };
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(EVENT_TIME_SYNC, content, options, SendOptions.SendReliable);
    }
    
    // Метод для запроса отката действия игрока
    public void RequestRollback(int targetPlayerId, int sequenceNumber, float duration)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        // Мастер-клиент может инициировать откат
        RollbackInfo rollback = new RollbackInfo
        {
            playerId = targetPlayerId,
            sequenceNumber = sequenceNumber,
            duration = duration,
            timestamp = PhotonNetwork.Time
        };
        
        // Добавляем в локальный список
        activeRollbacks.Add(rollback);
        
        // Отправляем всем клиентам
        object[] content = new object[] { 
            targetPlayerId, 
            sequenceNumber, 
            duration, 
            PhotonNetwork.Time
        };
        
        RaiseEventOptions options = new RaiseEventOptions { 
            Receivers = ReceiverGroup.All
        };
        
        PhotonNetwork.RaiseEvent(EVENT_ROLLBACK, content, options, SendOptions.SendReliable);
    }
    
    // Обработка сетевых событий
    public void OnEvent(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;
        
        switch (eventCode)
        {
            case EVENT_ROLLBACK:
                ProcessRollbackEvent(photonEvent);
                break;
                
            case EVENT_TIME_SYNC:
                ProcessTimeSyncEvent(photonEvent);
                break;
                
            case EVENT_TEAM_SELECTION:
                ProcessTeamSelectionEvent(photonEvent);
                break;
        }
    }
    
    private void ProcessTeamSelectionEvent(EventData photonEvent)
    {
        object[] data = (object[])photonEvent.CustomData;
        int playerActorNumber = (int)data[0];
        string team = (string)data[1];
        
    }
    
    private IEnumerator ApplyTeamColorWhenReady(string team)
    {
        // Ждем пока игрок будет инстанциирован
        while (GetLocalPlayerView() == null)
        {
            yield return new WaitForSeconds(0.2f);
        }
        
    }
    
    private PhotonView GetLocalPlayerView()
    {
        PhotonView[] views = FindObjectsOfType<PhotonView>();
        foreach (var view in views)
        {
            if (view.IsMine && view.Owner == PhotonNetwork.LocalPlayer)
            {
                return view;
            }
        }
        return null;
    }
    
    private void ProcessRollbackEvent(EventData photonEvent)
    {
        object[] data = (object[])photonEvent.CustomData;
        
        int targetPlayerId = (int)data[0];
        int sequenceNumber = (int)data[1];
        float duration = (float)data[2];
        double timestamp = (double)data[3];
        
        // Если это откат для текущего игрока
        if (targetPlayerId == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            // Находим объект игрока и применяем откат
            EgidaMain[] egidas = FindObjectsOfType<EgidaMain>();
            foreach (var egida in egidas)
            {
                if (egida.photonView.IsMine)
                {
                    egida.ApplyMovementRollback(sequenceNumber, duration);
                    break;
                }
            }
            
            MolniaMain[] molnias = FindObjectsOfType<MolniaMain>();
            foreach (var molnia in molnias)
            {
                if (molnia.photonView.IsMine)
                {
                    // Если у MolniaMain нет метода ApplyMovementRollback, его нужно добавить
                    molnia.ApplyMovementRollback(sequenceNumber, duration);
                    break;
                }
            }
        }
        
        // Сохраняем информацию об откате
        RollbackInfo rollback = new RollbackInfo
        {
            playerId = targetPlayerId,
            sequenceNumber = sequenceNumber,
            duration = duration,
            timestamp = timestamp
        };
        
        activeRollbacks.Add(rollback);
        
        // Очищаем завершенные откаты (старше maxRollbackTime)
        CleanupExpiredRollbacks();
    }
    
    private void ProcessTimeSyncEvent(EventData photonEvent)
    {
        object[] data = (object[])photonEvent.CustomData;
        double localTimeSent = (double)data[0];
        
        // Если мы мастер-клиент, отправляем ответ с серверным временем
        if (PhotonNetwork.IsMasterClient && photonEvent.Sender != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            object[] response = new object[] { 
                localTimeSent, 
                PhotonNetwork.Time,
                photonEvent.Sender
            };
            
            RaiseEventOptions options = new RaiseEventOptions { 
                TargetActors = new int[] { photonEvent.Sender }
            };
            
            PhotonNetwork.RaiseEvent(EVENT_TIME_SYNC, response, options, SendOptions.SendReliable);
        }
        // Если это ответ от мастера для нас
        else if (data.Length > 2 && (int)data[2] == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            double returnTime = Time.time;
            double roundTripTime = returnTime - localTimeSent;
            double serverTime = (double)data[1];
            
            // Предполагаем, что половина пути заняла половину времени
            double estimatedOneWayPing = roundTripTime / 2.0;
            
            // Корректируем локальное время
            double correctedServerTime = serverTime + estimatedOneWayPing;
            localTimeOffset = (float)(correctedServerTime - Time.time);
            
            // Сохраняем измерение пинга для среднего значения
            AddPingMeasurement(estimatedOneWayPing);
            
            lastTimeSync = Time.time;
            
            Debug.Log($"Синхронизация времени: коррекция {localTimeOffset}s, пинг {estimatedOneWayPing * 1000}мс");
        }
    }
    
    private void AddPingMeasurement(double ping)
    {
        pingMeasurements.Add(ping);
        
        if (pingMeasurements.Count > maxPingMeasurements)
        {
            pingMeasurements.RemoveAt(0);
        }
    }
    
    public double GetEstimatedServerTime()
    {
        // Возвращаем локальное время скорректированное на основе синхронизации с сервером
        return Time.time + localTimeOffset;
    }
    
    public double GetAveragePing()
    {
        if (pingMeasurements.Count == 0) return 0;
        
        double sum = 0;
        foreach (var ping in pingMeasurements)
        {
            sum += ping;
        }
        
        return sum / pingMeasurements.Count;
    }
    
    private void CleanupExpiredRollbacks()
    {
        double currentTime = PhotonNetwork.Time;
        
        activeRollbacks.RemoveAll(rollback => 
            currentTime > rollback.timestamp + rollback.duration + maxRollbackTime);
    }
    
    public bool IsInRollback(int playerId)
    {
        double currentTime = PhotonNetwork.Time;
        
        foreach (var rollback in activeRollbacks)
        {
            if (rollback.playerId == playerId && 
                currentTime < rollback.timestamp + rollback.duration)
            {
                return true;
            }
        }
        
        return false;
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
        
        if (timeResyncCoroutine != null)
        {
            StopCoroutine(timeResyncCoroutine);
            timeResyncCoroutine = null;
        }
    }
}

