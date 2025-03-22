using Photon.Pun;
using UnityEngine;
using ExitGames.Client.Photon;
using Photon.Realtime;
using System.Collections.Generic;

public interface INetworkEventHandler
{
    void OnNetworkEvent(byte eventCode, object[] data);
}

public class NetworkEvents : MonoBehaviourPunCallbacks
{
    private static NetworkEvents _instance;
    public static NetworkEvents Instance { get => _instance; }
    
    // Улучшенный кэш для обработчиков событий
    private Dictionary<byte, List<INetworkEventHandler>> eventHandlers = new Dictionary<byte, List<INetworkEventHandler>>();
    
    // Пулы объектов для снижения количества создаваемых объектов
    private Dictionary<string, Queue<object[]>> eventDataPool = new Dictionary<string, Queue<object[]>>();
    
    // Очередь событий для обработки в следующем кадре (снижает нагрузку)
    private Queue<KeyValuePair<byte, object[]>> eventQueue = new Queue<KeyValuePair<byte, object[]>>();
    
    // Ограничение скорости отправки событий для предотвращения спама
    private Dictionary<byte, float> eventRateLimits = new Dictionary<byte, float>();
    private Dictionary<byte, float> lastEventSentTime = new Dictionary<byte, float>();
    
    public enum EventCodes
    {
        PlayerDamaged = 1,
        PlayerHealed = 2,
        GameStateChanged = 3,
        RoundEnd = 4,
        SyncPosition = 5,
        PlayerAction = 6
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Устанавливаем ограничения скорости для разных типов событий
        eventRateLimits[(byte)EventCodes.PlayerDamaged] = 0.1f;
        eventRateLimits[(byte)EventCodes.PlayerHealed] = 0.1f;
        eventRateLimits[(byte)EventCodes.GameStateChanged] = 0.5f;
        eventRateLimits[(byte)EventCodes.RoundEnd] = 1.0f;
        eventRateLimits[(byte)EventCodes.SyncPosition] = 0.05f;
        eventRateLimits[(byte)EventCodes.PlayerAction] = 0.02f;
    }

    private void OnEnable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
    }

    private void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
    }
    
    private void Update()
    {
        // Обрабатываем очередь событий в Update для снижения нагрузки на сеть
        int processCount = 0;
        int maxEventsPerFrame = 10; // Ограничиваем количество обработанных событий за кадр
        
        while (eventQueue.Count > 0 && processCount < maxEventsPerFrame)
        {
            var eventData = eventQueue.Dequeue();
            ProcessEvent(eventData.Key, eventData.Value);
            processCount++;
        }
    }

    private void OnEvent(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;
        object[] data = (object[])photonEvent.CustomData;
        
        // Помещаем событие в очередь вместо немедленной обработки
        eventQueue.Enqueue(new KeyValuePair<byte, object[]>(eventCode, data));
    }
    
    private void ProcessEvent(byte eventCode, object[] data)
    {
        // Обработка события через систему обработчиков
        if (eventHandlers.TryGetValue(eventCode, out List<INetworkEventHandler> handlers))
        {
            for (int i = 0; i < handlers.Count; i++)
            {
                handlers[i].OnNetworkEvent(eventCode, data);
            }
        }
        
        // Обработка базовых событий
        switch (eventCode)
        {
            case (byte)EventCodes.PlayerDamaged:
                HandlePlayerDamaged(data);
                break;
            case (byte)EventCodes.PlayerHealed:
                HandlePlayerHealed(data);
                break;
            case (byte)EventCodes.GameStateChanged:
                HandleGameStateChanged(data);
                break;
            case (byte)EventCodes.RoundEnd:
                HandleRoundEnd(data);
                break;
        }
    }
    
    // Регистрация обработчиков событий
    public void RegisterEventHandler(EventCodes eventCode, INetworkEventHandler handler)
    {
        byte code = (byte)eventCode;
        if (!eventHandlers.TryGetValue(code, out List<INetworkEventHandler> handlers))
        {
            handlers = new List<INetworkEventHandler>();
            eventHandlers[code] = handlers;
        }
        
        if (!handlers.Contains(handler))
        {
            handlers.Add(handler);
        }
    }
    
    // Отмена регистрации обработчиков
    public void UnregisterEventHandler(EventCodes eventCode, INetworkEventHandler handler)
    {
        byte code = (byte)eventCode;
        if (eventHandlers.TryGetValue(code, out List<INetworkEventHandler> handlers))
        {
            handlers.Remove(handler);
        }
    }

    private void HandlePlayerDamaged(object[] data)
    {
        string playerID = (string)data[0];
        float damage = (float)data[1];
        // Обработка урона
        Debug.Log($"Player {playerID} took {damage} damage");
    }

    private void HandlePlayerHealed(object[] data)
    {
        string playerID = (string)data[0];
        float healAmount = (float)data[1];
        // Обработка лечения
        Debug.Log($"Player {playerID} healed for {healAmount}");
    }

    private void HandleGameStateChanged(object[] data)
    {
        string newState = (string)data[0];
        // Обработка изменения состояния игры
        Debug.Log($"Game state changed to: {newState}");
    }

    private void HandleRoundEnd(object[] data)
    {
        string winningTeam = (string)data[0];
        // Обработка окончания раунда
        Debug.Log($"Round ended. Winner: {winningTeam}");
    }
    
    // Получение объекта данных из пула
    private object[] GetEventData(string key, int size)
    {
        if (!eventDataPool.TryGetValue(key, out Queue<object[]> pool))
        {
            pool = new Queue<object[]>();
            eventDataPool[key] = pool;
        }
        
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }
        
        return new object[size];
    }
    
    // Возврат объекта данных в пул
    private void RecycleEventData(string key, object[] data)
    {
        if (!eventDataPool.TryGetValue(key, out Queue<object[]> pool))
        {
            pool = new Queue<object[]>();
            eventDataPool[key] = pool;
        }
        
        pool.Enqueue(data);
    }

    // Методы для отправки событий с оптимизацией и контролем частоты
    public void SendPlayerDamaged(string playerID, float damage)
    {
        byte eventCode = (byte)EventCodes.PlayerDamaged;
        if (CanSendEvent(eventCode))
        {
            string key = "playerDamaged";
            object[] content = GetEventData(key, 2);
            content[0] = playerID;
            content[1] = damage;
            
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            SendOptions sendOptions = SendOptions.SendReliable;
            
            PhotonNetwork.RaiseEvent(eventCode, content, raiseEventOptions, sendOptions);
            RecycleEventData(key, content);
            
            // Запоминаем время отправки
            lastEventSentTime[eventCode] = Time.time;
        }
    }

    public void SendPlayerHealed(string playerID, float healAmount)
    {
        byte eventCode = (byte)EventCodes.PlayerHealed;
        if (CanSendEvent(eventCode))
        {
            string key = "playerHealed";
            object[] content = GetEventData(key, 2);
            content[0] = playerID;
            content[1] = healAmount;
            
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(eventCode, content, raiseEventOptions, SendOptions.SendReliable);
            RecycleEventData(key, content);
            
            lastEventSentTime[eventCode] = Time.time;
        }
    }

    public void SendGameStateChanged(string newState)
    {
        byte eventCode = (byte)EventCodes.GameStateChanged;
        if (CanSendEvent(eventCode))
        {
            string key = "gameState";
            object[] content = GetEventData(key, 1);
            content[0] = newState;
            
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(eventCode, content, raiseEventOptions, SendOptions.SendReliable);
            RecycleEventData(key, content);
            
            lastEventSentTime[eventCode] = Time.time;
        }
    }

    public void SendRoundEnd(string winningTeam)
    {
        byte eventCode = (byte)EventCodes.RoundEnd;
        if (CanSendEvent(eventCode))
        {
            string key = "roundEnd";
            object[] content = GetEventData(key, 1);
            content[0] = winningTeam;
            
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(eventCode, content, raiseEventOptions, SendOptions.SendReliable);
            RecycleEventData(key, content);
            
            lastEventSentTime[eventCode] = Time.time;
        }
    }
    
    // Метод для быстрой синхронизации позиции (с ненадежной доставкой для скорости)
    public void SendSyncPosition(string playerID, Vector3 position, Quaternion rotation, Vector3 velocity)
    {
        byte eventCode = (byte)EventCodes.SyncPosition;
        if (CanSendEvent(eventCode))
        {
            string key = "syncPos";
            object[] content = GetEventData(key, 4);
            content[0] = playerID;
            content[1] = position;
            content[2] = rotation;
            content[3] = velocity;
            
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { 
                Receivers = ReceiverGroup.Others,
                CachingOption = EventCaching.DoNotCache
            };
            
            // Используем ненадежную доставку для частых обновлений позиции
            PhotonNetwork.RaiseEvent(eventCode, content, raiseEventOptions, SendOptions.SendUnreliable);
            RecycleEventData(key, content);
            
            lastEventSentTime[eventCode] = Time.time;
        }
    }
    
    // Проверка возможности отправки события (контроль частоты)
    private bool CanSendEvent(byte eventCode)
    {
        if (!eventRateLimits.TryGetValue(eventCode, out float rateLimit))
        {
            return true;
        }
        
        if (!lastEventSentTime.TryGetValue(eventCode, out float lastSent))
        {
            return true;
        }
        
        return (Time.time - lastSent) >= rateLimit;
    }
} 