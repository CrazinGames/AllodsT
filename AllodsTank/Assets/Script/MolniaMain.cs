using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using static OneUpdate;

public class MolniaMain : MonoBehaviourPunCallbacks, IPunObservable, IUpdatable
{
    [Header("References")]
    [SerializeField] private GameObject[] obj; // 0 - основной объект, 1 - оружие, 2 - точка привязки
    [SerializeField] private StatsMount stat;
    [SerializeField] private float rotationSpeed2 = 10f;

    [Header("Network")]
    [SerializeField] private float baseSmoothing = 15f; // Базовое значение сглаживания
    [SerializeField] private float teleportDistanceThreshold = 3f;
    [SerializeField] private float minSmoothing = 5f; // Минимальное сглаживание при высоком пинге
    [SerializeField] private float maxSmoothing = 20f; // Максимальное сглаживание при низком пинге
    [SerializeField] private int bufferSize = 20; // Размер буфера истории движений
    [SerializeField] private float maxPredictionTime = 1.0f; // Максимальное время предсказания

    private OneUpdate oneUpdate;
    private CameraMove cam;
    private Camera mainCam;
    private bool isInitialized = false;
    private float currentSmoothing; // Текущее динамическое сглаживание

    // Сетевые переменные для интерполяции
    private Vector3 correctPlayerPos;
    private Quaternion correctPlayerRot;
    
    // Структура для хранения состояния движения
    private struct MovementState
    {
        public Vector3 position;
        public Quaternion rotation;
        public double timestamp; // Серверное время
    }
    
    // Буфер для хранения состояний движения
    private List<MovementState> movementBuffer = new List<MovementState>();
    
    // Информация о последней команде
    private double lastInputTime;
    private int lastSequenceNumber = 0;
    private int currentSequenceNumber = 0;
    
    // Структура для хранения информации об откатах движения
    private struct MovementRollback
    {
        public int sequenceNumber;
        public float duration;
        public double startTimestamp;
        public bool isActive;
    }
    
    // Список активных откатов
    private List<MovementRollback> activeRollbacks = new List<MovementRollback>();

    private void Awake()
    {
        correctPlayerPos = transform.position;
        correctPlayerRot = transform.rotation;
        currentSmoothing = baseSmoothing;
        
        // Инициализация буфера движений
        for (int i = 0; i < bufferSize; i++)
        {
            movementBuffer.Add(new MovementState
            {
                position = transform.position,
                rotation = transform.rotation,
                timestamp = PhotonNetwork.Time
            });
        }
    }

    private void Start()
    {
        if (!photonView.IsMine) return;

        mainCam = Camera.main;
        if (mainCam == null) Debug.LogError("Main camera not found!");

        cam = mainCam.GetComponent<CameraMove>();
        if (cam == null) Debug.LogError("CameraMove not found on main camera!");

        oneUpdate = FindAnyObjectByType<OneUpdate>();
        if (oneUpdate == null) Debug.LogError("OneUpdate not found in scene!");
        else oneUpdate.RegisterUpdatable(this);

        isInitialized = (obj[0] != null);
        if (!isInitialized) Debug.LogError("Not all objects initialized!");
    }

    private void OnDestroy()
    {
        if (photonView.IsMine && oneUpdate != null)
            oneUpdate.UnregisterUpdatable(this);
    }

    void IUpdatable.CustomFixedUpdate()
    {
        if (!isInitialized) return;

        if (photonView.IsMine)
        {
            // Проверка активных откатов
            HandleActiveRollbacks();
            
            // Регистрация нового состояния
            currentSequenceNumber++;
            lastInputTime = PhotonNetwork.Time;
            
            // Выполнение движения
            MainObj();
            //stat.HP(obj[0]);
            // Сохранение состояния в буфер
            SaveCurrentState();
            
            if (cam != null) cam.camMove(false);
        }
        else
        {
            ApplyNetworkInterpolation();
        }
    }
    
    private void HandleActiveRollbacks()
    {
        // Удаление завершенных откатов
        activeRollbacks.RemoveAll(r => 
            !r.isActive || (PhotonNetwork.Time >= r.startTimestamp + r.duration));
            
        // Применение активных откатов
        foreach (var rollback in activeRollbacks)
        {
            if (rollback.isActive)
            {
                // Здесь логика применения эффектов отката
                // Например, замедление движения на время действия отката
                
                // float rollbackProgress = (float)((PhotonNetwork.Time - rollback.startTimestamp) / rollback.duration);
                // Применение эффекта отката...
            }
        }
    }
    
    private void SaveCurrentState()
    {
        // Добавление нового состояния в буфер и удаление самого старого
        if (movementBuffer.Count >= bufferSize)
        {
            movementBuffer.RemoveAt(0);
        }
        
        movementBuffer.Add(new MovementState
        {
            position = obj[0].transform.position,
            rotation = obj[0].transform.rotation,
            timestamp = PhotonNetwork.Time
        });
    }

    private void MainObj()
    {
        Vector3 movement = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
            movement = stat._speed * Time.deltaTime * obj[0].transform.up;
        else if (Input.GetKey(KeyCode.S))
            movement = stat._speed * Time.deltaTime * -obj[0].transform.up;

        if (Input.GetKey(KeyCode.A))
            obj[0].transform.Rotate(Vector3.forward, stat._speedRot * Time.deltaTime);
        else if (Input.GetKey(KeyCode.D))
            obj[0].transform.Rotate(Vector3.forward, -stat._speedRot * Time.deltaTime);

        if (movement != Vector3.zero)
            obj[0].transform.position += movement;
    }


    private void ApplyNetworkInterpolation()
    {
        // Получение времени с учетом задержки сети (пинга)
        double interpolationTime = PhotonNetwork.Time - (PhotonNetwork.GetPing() * 0.001);
        
        // Поиск состояний для интерполяции
        MovementState older = default;
        MovementState newer = default;
        bool foundStates = false;
        
        // Поиск двух состояний для интерполяции на основе времени
        for (int i = 0; i < movementBuffer.Count - 1; i++)
        {
            if (movementBuffer[i].timestamp <= interpolationTime && 
                movementBuffer[i + 1].timestamp >= interpolationTime)
            {
                older = movementBuffer[i];
                newer = movementBuffer[i + 1];
                foundStates = true;
                break;
            }
        }
        
        // Если не нашли подходящие состояния, используем последнее известное
        if (!foundStates && movementBuffer.Count > 0)
        {
            // Телепорт при большой десинхронизации
            if (Vector3.Distance(obj[0].transform.position, correctPlayerPos) > teleportDistanceThreshold)
            {
                obj[0].transform.SetPositionAndRotation(correctPlayerPos, correctPlayerRot);
                return;
            }
            
            // Интерполяция к последнему известному состоянию
            obj[0].transform.position = Vector3.Lerp(obj[0].transform.position, correctPlayerPos, Time.deltaTime * currentSmoothing);
            obj[0].transform.rotation = Quaternion.Slerp(obj[0].transform.rotation, correctPlayerRot, Time.deltaTime * currentSmoothing);
            return;
        }
        
        if (foundStates)
        {
            // Расчет коэффициента интерполяции
            float t = (float)((interpolationTime - older.timestamp) / (newer.timestamp - older.timestamp));
            
            // Применение интерполяции
            obj[0].transform.position = Vector3.Lerp(older.position, newer.position, t);
            obj[0].transform.rotation = Quaternion.Slerp(older.rotation, newer.rotation, t);
        }
    }
    
    // Метод для применения отката движения по запросу от сервера
    public void ApplyMovementRollback(int sequenceNumber, float duration)
    {
        if (!photonView.IsMine) return;
        
        MovementRollback rollback = new MovementRollback
        {
            sequenceNumber = sequenceNumber,
            duration = duration,
            startTimestamp = PhotonNetwork.Time,
            isActive = true
        };
        
        activeRollbacks.Add(rollback);
        
        // Отправка RPC для визуализации эффекта другим игрокам
        photonView.RPC("ShowRollbackEffect", RpcTarget.Others, PhotonNetwork.Time, duration);
    }
    
    [PunRPC]
    private void ShowRollbackEffect(double startTime, float duration)
    {
        // Код для отображения визуального эффекта отката другим игрокам
        // Например, изменение цвета, эффект замедления и т.д.
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Отправка позиции, вращения и последовательного номера
            stream.SendNext(obj[0].transform.position);
            stream.SendNext(obj[0].transform.rotation);
            stream.SendNext(currentSequenceNumber);
            stream.SendNext(PhotonNetwork.Time); // Отправка текущего серверного времени
        }
        else
        {
            // Получение данных
            correctPlayerPos = (Vector3)stream.ReceiveNext();
            correctPlayerRot = (Quaternion)stream.ReceiveNext();
            int receivedSequence = (int)stream.ReceiveNext();
            double timestamp = (double)stream.ReceiveNext();
            
            // Добавление полученного состояния в буфер
            if (movementBuffer.Count >= bufferSize)
            {
                movementBuffer.RemoveAt(0);
            }
            
            movementBuffer.Add(new MovementState
            {
                position = correctPlayerPos,
                rotation = correctPlayerRot,
                timestamp = timestamp
            });

            // Динамический расчет smoothing на основе пинга
            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            currentSmoothing = Mathf.Clamp(baseSmoothing / (1f + lag), minSmoothing, maxSmoothing);
        }
    }
}