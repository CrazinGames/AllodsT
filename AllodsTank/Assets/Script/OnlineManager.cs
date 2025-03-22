using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using System.Collections;

public class OnlineManager : MonoBehaviourPunCallbacks
{
    public static OnlineManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_InputField createRoomInput;
    [SerializeField] private TMP_InputField joinRoomInput;
    [SerializeField] private TMP_InputField nickNameInput;
    [SerializeField] private Button[] mountButtons;
    [SerializeField] private GameObject connectionStatusPanel;
    [SerializeField] private TMP_Text connectionStatusText;
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    
    [Header("Game Settings")]
    [SerializeField] private string gameVersion = "1.0";
    [SerializeField] private byte maxPlayersPerRoom = 4;
    [SerializeField] internal MountName mountName;
    [SerializeField] private float reconnectDelay = 5f;
    [SerializeField] private int maxReconnectAttempts = 3;
    [SerializeField] private bool useBestRegion = true; // Новый параметр для выбора метода подключения
    
    // Настройки оптимизации сети
    [Header("Network Optimization")]
    [SerializeField] private bool useNatPunchthrough = true;
    [SerializeField] private int sendRateOnSerialize = 30; // Частота отправки обновлений (раз в секунду)
    [SerializeField] private byte serializedViewsInitial = 10; // Максимальное начальное количество объектов для синхронизации
    [SerializeField] private int photonPingInterval = 500; // Интервал отправки пингов в миллисекундах
    
    // Статус подключения и попытки переподключения
    private bool isConnecting = false;
    private int reconnectAttempts = 0;
    private Coroutine reconnectCoroutine;
    
    // Список доступных серверов с пингом
    private List<Region> availableRegions = new List<Region>();
    private Region bestRegion;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            // Находим UI-элементы, которые должны быть уничтожены при переходе на новую сцену
            Transform canvasParent = null;
            if (connectionStatusPanel != null)
                canvasParent = connectionStatusPanel.transform.parent;
            else if (errorPanel != null)
                canvasParent = errorPanel.transform.parent;
            else if (createRoomInput != null)
                canvasParent = createRoomInput.transform.parent;
            
            // Только сам OnlineManager сохраняем между сценами
            DontDestroyOnLoad(gameObject);
            
            // Отделяем UI от OnlineManager, чтобы он не сохранялся между сценами
            if (canvasParent != null && canvasParent.GetComponent<Canvas>() != null)
            {
                Debug.Log("Menu UI canvas found, it will be destroyed when changing scenes");
                
                // Отделяем от DontDestroyOnLoad
                Canvas canvas = canvasParent.GetComponent<Canvas>();
                if (canvas != null && canvas.transform.parent != null)
                {
                    canvas.transform.SetParent(null);
                }
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Проверяем и инициализируем необходимые компоненты
        if (mountName == null)
        {
            Debug.LogError("MountName component is not assigned in the inspector!");
            mountName = ScriptableObject.CreateInstance<MountName>();
            mountName._mountName = "Egida";
        }
        
        // Находим ссылки на кнопки, если они не назначены
        FindRoomButtonsIfNeeded();
        
        // Устанавливаем значения для оптимизации сети
        PhotonNetwork.SendRate = sendRateOnSerialize;
        PhotonNetwork.SerializationRate = sendRateOnSerialize;
        
        // Устанавливаем начальное значение для количества синхронизируемых объектов
        PhotonNetwork.PrecisionForVectorSynchronization = 999; // Настройка точности для векторов (меньше = меньше данных, но меньше точность)
        PhotonNetwork.PrecisionForQuaternionSynchronization = 1000; // Настройка точности для кватернионов
        PhotonNetwork.PrecisionForFloatSynchronization = 1000; // Настройка точности для float
        
        // Устанавливаем интервал пинга
        PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout = 10000; // 10 секунд до таймаута
        PhotonNetwork.NetworkingClient.LoadBalancingPeer.DebugOut = ExitGames.Client.Photon.DebugLevel.ERROR; // Уровень логирования
        
        if (useNatPunchthrough)
        {
            PhotonNetwork.UseRpcMonoBehaviourCache = true;
        }
    }

    private void FindRoomButtonsIfNeeded()
    {
        // Если кнопки не назначены, попробуем найти их через Input Fields
        if (createRoomButton == null && createRoomInput != null)
        {
            // Ищем кнопку в родительском объекте или в соседних объектах
            Transform parent = createRoomInput.transform.parent;
            if (parent != null)
            {
                // Сначала ищем кнопку, которая может находиться рядом с Input Field
                createRoomButton = parent.GetComponentInChildren<Button>();
                
                // Если не найдена, ищем кнопку с определенными именами
                if (createRoomButton == null)
                {
                    Button[] buttons = parent.GetComponentsInChildren<Button>();
                    foreach (Button btn in buttons)
                    {
                        if (btn.name.ToLower().Contains("create") || btn.name.ToLower().Contains("creat"))
                        {
                            createRoomButton = btn;
                            break;
                        }
                    }
                }
            }
        }
        
        if (joinRoomButton == null && joinRoomInput != null)
        {
            // Аналогично для кнопки присоединения
            Transform parent = joinRoomInput.transform.parent;
            if (parent != null)
            {
                // Исключаем кнопку создания комнаты, если она уже найдена
                Button[] buttons = parent.GetComponentsInChildren<Button>();
                foreach (Button btn in buttons)
                {
                    if (btn != createRoomButton && (btn.name.ToLower().Contains("join") || btn.name.ToLower().Contains("enter")))
                    {
                        joinRoomButton = btn;
                        break;
                    }
                }
                
                // Если не найдена по имени, берем любую доступную кнопку, отличную от createRoomButton
                if (joinRoomButton == null)
                {
                    foreach (Button btn in buttons)
                    {
                        if (btn != createRoomButton)
                        {
                            joinRoomButton = btn;
                            break;
                        }
                    }
                }
            }
        }
        
        Debug.Log($"Found buttons: Create Room: {(createRoomButton != null ? createRoomButton.name : "null")}, Join Room: {(joinRoomButton != null ? joinRoomButton.name : "null")}");
    }

    private void SetRoomButtonsInteractable(bool interactable)
    {
        // Если кнопки не были назначены, попытаемся найти их
        if ((createRoomButton == null || joinRoomButton == null) && (createRoomInput != null || joinRoomInput != null))
        {
            FindRoomButtonsIfNeeded();
        }
        
        // Если кнопки все еще null, попробуем прямой способ через имена
        if (createRoomButton == null || joinRoomButton == null)
        {
            Button[] allButtons = FindObjectsOfType<Button>();
            
            foreach (Button btn in allButtons)
            {
                // Активируем все кнопки, которые могут быть связаны с созданием/присоединением к комнате
                string btnName = btn.name.ToLower();
                
                if (createRoomButton == null && (btnName.Contains("create") || btnName.Contains("creat")))
                {
                    createRoomButton = btn;
                }
                
                if (joinRoomButton == null && (btnName.Contains("join") || btnName.Contains("enter")))
                {
                    joinRoomButton = btn;
                }
            }
        }
        
        // Применяем настройки интерактивности
        if (createRoomButton != null)
        {
            createRoomButton.interactable = interactable;
            Debug.Log($"Set create room button interactable: {interactable}");
        }
        else
        {
            Debug.LogWarning("Create Room button not found!");
        }
            
        if (joinRoomButton != null)
        {
            joinRoomButton.interactable = interactable;
            Debug.Log($"Set join room button interactable: {interactable}");
        }
        else
        {
            Debug.LogWarning("Join Room button not found!");
        }
    }

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        
        // Отключаем проверку региона для прямого подключения
        useBestRegion = false;
        
        // Отключаем кнопки до полного подключения
        SetRoomButtonsInteractable(false);
        
        if (!PhotonNetwork.IsConnected)
        {
            isConnecting = true;
            UpdateConnectionStatus("Connecting to server...");
            ConnectToServer();
        }
        else if (PhotonNetwork.InLobby || PhotonNetwork.IsConnectedAndReady)
        {
            SetRoomButtonsInteractable(true);
        }

        // Проверяем наличие кнопок
        if (mountButtons == null || mountButtons.Length == 0)
        {
            Debug.LogError("Mount buttons are not assigned in the inspector!");
            return;
        }

        foreach (Button btn in mountButtons)
        {
            if (btn != null)
            {
                btn.onClick.AddListener(() => SelectMount(btn.gameObject));
            }
            else
            {
                Debug.LogWarning("One of the mount buttons is null!");
            }
        }
    }

    private void ConnectToServer()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Already connected to server, no need to reconnect");
            isConnecting = false;
            UpdateConnectionStatus("Connected to server");
            return;
        }
        
        isConnecting = true;
        
        // Прямое подключение к Master Server без поиска региона
        UpdateConnectionStatus("Connecting to Master Server...");
        
        // Устанавливаем версию игры
        PhotonNetwork.GameVersion = gameVersion;
        
        // Простое прямое подключение вместо поиска лучшего региона
        PhotonNetwork.ConnectUsingSettings();
        
        Debug.Log("Started connection to Master Server");
    }
    
    private void ConnectToBestRegion()
    {
        // Этот метод больше не используется, но оставлен для совместимости
        PhotonNetwork.ConnectUsingSettings();
    }
    
    // Метод для переподключения при потере соединения
    private void TryReconnect()
    {
        if (reconnectCoroutine != null)
        {
            StopCoroutine(reconnectCoroutine);
        }
        
        reconnectCoroutine = StartCoroutine(ReconnectCoroutine());
    }
    
    private IEnumerator ReconnectCoroutine()
    {
        while (reconnectAttempts < maxReconnectAttempts && !PhotonNetwork.IsConnected)
        {
            reconnectAttempts++;
            UpdateConnectionStatus($"Connection lost. Reconnecting ({reconnectAttempts}/{maxReconnectAttempts})...");
            
            yield return new WaitForSeconds(reconnectDelay);
            
            if (!PhotonNetwork.IsConnected)
            {
                ConnectToServer();
            }
        }
        
        if (!PhotonNetwork.IsConnected)
        {
            UpdateConnectionStatus("Failed to reconnect to server");
            ShowError("Failed to reconnect. Please check your internet connection and try again.");
        }
    }
    
    private void UpdateConnectionStatus(string status)
    {
        if (connectionStatusPanel != null)
        {
            connectionStatusPanel.SetActive(true);
            
            if (connectionStatusText != null)
            {
                connectionStatusText.text = status;
            }
        }
        
        Debug.Log($"Connection status: {status}");
    }
    
    private void ShowError(string error)
    {
        if (errorPanel != null)
        {
            errorPanel.SetActive(true);
            
            if (errorText != null)
            {
                errorText.text = error;
            }
        }
        
        Debug.LogError($"Network error: {error}");
    }
    
    public void HideError()
    {
        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }
    }

    public void SelectMount(GameObject buttonObj)
    {
        if (buttonObj == null)
        {
            Debug.LogError("Button object is null!");
            return;
        }

        if (mountName == null)
        {
            Debug.LogError("MountName component is null!");
            return;
        }

        mountName._mountName = buttonObj.tag;
        
        if (nickNameInput != null && !string.IsNullOrEmpty(nickNameInput.text))
        {
            PhotonNetwork.NickName = nickNameInput.text;
        }
        else
        {
            // Если никнейм не указан, используем дефолтное значение
            PhotonNetwork.NickName = "Player_" + Random.Range(1000, 9999);
            if (nickNameInput != null)
            {
                nickNameInput.text = PhotonNetwork.NickName;
            }
        }

        Debug.Log($"Selected mount: {mountName._mountName}, Player nickname: {PhotonNetwork.NickName}");
    }

    public void CreateRoom()
    {
        // Проверка подключения
        if (!PhotonNetwork.IsConnected)
        {
            ShowError("Not connected to server. Connecting...");
            ConnectToServer();
            return;
        }
        
        // Проверка, что мы на Master Server
        if (PhotonNetwork.NetworkClientState != ClientState.ConnectedToMasterServer && 
            PhotonNetwork.NetworkClientState != ClientState.JoinedLobby)
        {
            ShowError($"Cannot create room: Client is not connected to Master Server. Current state: {PhotonNetwork.NetworkClientState}");
            Debug.LogWarning($"Cannot create room: Client state is {PhotonNetwork.NetworkClientState}");
            
            // Если мы подключены к NameServer, но не к MasterServer, попробуем подключиться заново
            if (PhotonNetwork.NetworkClientState == ClientState.ConnectedToNameServer)
            {
                Debug.Log("Currently connected to NameServer, trying to connect to MasterServer...");
                // Прямое подключение к Master Server
                PhotonNetwork.GameVersion = gameVersion;
                PhotonNetwork.ConnectUsingSettings();
            }
            return;
        }
        
        if (string.IsNullOrEmpty(mountName._mountName))
        {
            mountName._mountName = "Egida";
        }

        if (string.IsNullOrWhiteSpace(createRoomInput.text))
        {
            ShowError("Room name cannot be empty");
            return;
        }

        Debug.Log("Creating room with name: " + createRoomInput.text);
        UpdateConnectionStatus("Creating room...");
        
        try
        {
            // Упрощенные настройки комнаты
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = maxPlayersPerRoom,
                PublishUserId = true,
                CleanupCacheOnLeave = true
            };

            // Добавляем основные кастомные свойства
            ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable
            {
                { "GameMode", "TeamDeathmatch" },
                { "MapName", "Default" },
                { "CreatorMount", mountName._mountName }
            };
            roomOptions.CustomRoomProperties = customProperties;
            
            // Создаем комнату
            bool result = PhotonNetwork.CreateRoom(createRoomInput.text, roomOptions);
            Debug.Log($"CreateRoom call result: {result}, Current state: {PhotonNetwork.NetworkClientState}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating room: {e.Message}, Current state: {PhotonNetwork.NetworkClientState}");
            ShowError($"Failed to create room: {e.Message}");
        }
    }

    public void JoinRoom()
    {
        if (!PhotonNetwork.IsConnected)
        {
            ShowError("Not connected to server. Please wait or restart the game.");
            return;
        }
        
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            ShowError("Still connecting to the server. Please wait for connection to complete.");
            return;
        }
        
        if (string.IsNullOrEmpty(mountName._mountName))
        {
            mountName._mountName = "Egida";
        }

        if (string.IsNullOrWhiteSpace(joinRoomInput.text))
        {
            ShowError("Room name cannot be empty");
            return;
        }

        UpdateConnectionStatus($"Joining room {joinRoomInput.text}...");
        
        try
        {
            PhotonNetwork.JoinRoom(joinRoomInput.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error joining room: {e.Message}");
            ShowError($"Failed to join room: {e.Message}");
        }
    }

    public void LeaveRoom()
    {
        UpdateConnectionStatus("Leaving room...");
        PhotonNetwork.LeaveRoom();
    }

    public void SetPlayerCustomProperties(Dictionary<string, object> properties)
    {
        if (PhotonNetwork.LocalPlayer != null)
        {
            ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
            foreach (var prop in properties)
            {
                customProperties[prop.Key] = prop.Value;
            }
            PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);
        }
    }
    
    // Метод для проверки пинга до сервера
    public int GetCurrentPing()
    {
        return PhotonNetwork.GetPing();
    }

    #region PUN Callbacks
    public override void OnConnectedToMaster()
    {
        isConnecting = false;
        reconnectAttempts = 0;
        Debug.Log("OnConnectedToMaster() was called by PUN - Fully connected to Master Server!");
        UpdateConnectionStatus("Connected to Master Server - Ready");
        
        if (reconnectCoroutine != null)
        {
            StopCoroutine(reconnectCoroutine);
            reconnectCoroutine = null;
        }
        
        // Сразу активируем кнопки создания/подключения к комнате
        SetRoomButtonsInteractable(true);
        
        // Выводим состояние подключения
        Debug.Log($"Client state: {PhotonNetwork.NetworkClientState}, Is connected and ready: {PhotonNetwork.IsConnectedAndReady}");
        
        // Входим в лобби (опционально, для простых приложений может не требоваться)
        // В большинстве случаев после подключения к Master Server уже можно создавать/присоединяться к комнатам
        if (!PhotonNetwork.InLobby)
        {
            Debug.Log("Joining lobby...");
            PhotonNetwork.JoinLobby();
        }
        else
        {
            Debug.Log("Already in lobby");
        }
        
        // Скрываем панель статуса после короткой задержки
        if (connectionStatusPanel != null)
        {
            StartCoroutine(HideStatusPanelAfterDelay(2f));
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Successfully joined lobby - Ready for matchmaking");
        UpdateConnectionStatus("Joined Lobby - Ready to play");
        
        // Явно активируем кнопки здесь
        SetRoomButtonsInteractable(true);
        
        if (connectionStatusPanel != null)
        {
            StartCoroutine(HideStatusPanelAfterDelay(2f));
        }
    }
    
    private IEnumerator HideStatusPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (connectionStatusPanel != null)
        {
            connectionStatusPanel.SetActive(false);
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined Room: {PhotonNetwork.CurrentRoom.Name}");
        UpdateConnectionStatus($"Joined Room: {PhotonNetwork.CurrentRoom.Name}");
        
        // Синхронизация свойств игрока
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { "MountName", mountName._mountName },
            { "Ready", false }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        
        // Скрываем UI меню перед загрузкой уровня
        HideMenuUI();
        
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Game");
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Player {newPlayer.NickName} joined the room");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Player {otherPlayer.NickName} left the room");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Room creation failed: {message}");
        ShowError($"Failed to create room: {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Failed to join room: {message}");
        ShowError($"Failed to join room: {message}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Disconnected: {cause}");
        UpdateConnectionStatus($"Disconnected: {cause}");
        
        isConnecting = false;
        
        SetRoomButtonsInteractable(false);
        
        if (cause != DisconnectCause.DisconnectByClientLogic)
        {
            TryReconnect();
        }
    }
    
    public override void OnRegionListReceived(RegionHandler regionHandler)
    {
        availableRegions = regionHandler.EnabledRegions;
        
        // Находим регион с лучшим пингом
        bestRegion = regionHandler.BestRegion;
        
        if (availableRegions.Count > 0)
        {
            string regionInfo = "Available regions:";
            foreach (Region region in availableRegions)
            {
                regionInfo += $"\n{region.Code}: {region.Ping}ms";
            }
            Debug.Log(regionInfo);
        }
        
        ConnectToBestRegion();
    }
    
    public override void OnCustomAuthenticationFailed(string debugMessage)
    {
        Debug.LogError($"Custom authentication failed: {debugMessage}");
        ShowError($"Authentication failed: {debugMessage}");
    }
    
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"Room list updated: {roomList.Count} rooms available");
    }
    #endregion

    public void ToggleConnectionMethod()
    {
        useBestRegion = !useBestRegion;
        Debug.Log($"Connection method changed: Use Best Region = {useBestRegion}");
        
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("You need to restart the game or disconnect to apply the new connection method");
        }
    }
    
    public void ForceReconnect()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            StartCoroutine(ReconnectAfterDisconnect());
        }
        else
        {
            ConnectToServer();
        }
    }
    
    private IEnumerator ReconnectAfterDisconnect()
    {
        // Ждем отключения
        while (PhotonNetwork.IsConnected)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Небольшая задержка перед переподключением
        yield return new WaitForSeconds(1.0f);
        
        // Переподключаемся
        ConnectToServer();
    }

    // Метод для скрытия UI меню
    private void HideMenuUI()
    {
        // Находим и скрываем все UI элементы меню
        Transform[] menuElements = {
            connectionStatusPanel?.transform,
            errorPanel?.transform,
            createRoomInput?.transform.parent,
            joinRoomInput?.transform.parent,
            nickNameInput?.transform.parent
        };
        
        foreach (var element in menuElements)
        {
            if (element != null)
            {
                // Ищем родительский Canvas и скрываем его
                Transform parent = element;
                Canvas canvas = null;
                
                // Поднимаемся по иерархии, пока не найдем Canvas
                while (parent != null)
                {
                    canvas = parent.GetComponent<Canvas>();
                    if (canvas != null)
                    {
                        canvas.gameObject.SetActive(false);
                        Debug.Log($"Hiding menu canvas: {canvas.name}");
                        break;
                    }
                    
                    if (parent.parent == null)
                        break;
                        
                    parent = parent.parent;
                }
                
                // Если Canvas не найден, скрываем сам элемент
                if (canvas == null && element.gameObject != null)
                {
                    element.gameObject.SetActive(false);
                }
            }
        }
    }
}

