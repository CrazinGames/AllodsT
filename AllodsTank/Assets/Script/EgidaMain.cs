using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using static OneUpdate;

public class EgidaMain : MonoBehaviourPunCallbacks, IPunObservable, IUpdatable
{
    [Header("References")]
    [SerializeField] private GameObject[] obj; // 0 - основной объект, 1 - оружие, 2 - точка привязки
    [SerializeField] private StatsMount stat;
    [SerializeField] private EgidaFire fire;
    [SerializeField] private float rotationSpeed2 = 10f;
    [SerializeField] private float smoothing = 10f;

    [Header("Network")]
    [SerializeField] private float teleportDistanceThreshold = 5f; // Порог для телепорта при десинхронизации
    [SerializeField] private int bufferSize = 20; // Размер буфера для хранения истории движений
    [SerializeField] private float maxPredictionTime = 1.0f;
    [SerializeField] private PhotonView view;// Максимальное время предсказания в секундах

    private OneUpdate oneUpdate;
    private CameraMove cam;
    private Camera mainCam;
    private bool isInitialized = false;

    // Сетевые переменные для интерполяции
    private Vector3 correctPlayerPos;
    private Quaternion correctPlayerRot;
    private Quaternion correctWeaponRot;

    [SerializeField] private Animator _animator;
    [SerializeField] private Collider2D _collision;

    // Структура для хранения состояния движения
    private struct MovementState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Quaternion weaponRotation;
        public double timestamp; // Серверное время
    }
    
    // Буфер для хранения состояний движения
    private List<MovementState> movementBuffer = new List<MovementState>();
    
    // Информация о последней команде, отправленной на сервер
    private double lastInputTime;
    private int lastSequenceNumber = 0;
    private int currentSequenceNumber = 0;
    
    // Отката движения
    private struct MovementRollback
    {
        public int sequenceNumber;
        public float duration;
        public double startTimestamp;
        public bool isActive;
    }
    
    private List<MovementRollback> activeRollbacks = new List<MovementRollback>();

    private void Awake()
    {
        correctPlayerPos = transform.position;
        correctPlayerRot = transform.rotation;
        correctWeaponRot = obj.Length > 1 ? obj[1].transform.rotation : Quaternion.identity;
        
        // Инициализация буфера движений
        for (int i = 0; i < bufferSize; i++)
        {
            movementBuffer.Add(new MovementState
            {
                position = transform.position,
                rotation = transform.rotation,
                weaponRotation = obj.Length > 1 ? obj[1].transform.rotation : Quaternion.identity,
                timestamp = PhotonNetwork.Time
            });
        }
    }



    private void Start()
    {

        if (!photonView.IsMine) return;
        view = GetComponentInParent<PhotonView>();

        mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("Main camera not found!");
            return;
        }

        cam = mainCam.GetComponent<CameraMove>();
        if (cam == null)
            Debug.LogError("CameraMove not found on main camera!");

        oneUpdate = FindAnyObjectByType<OneUpdate>();
        if (oneUpdate == null)
            Debug.LogError("OneUpdate not found in scene!");
        else
            oneUpdate.RegisterUpdatable(this);

        // Проверка всех ссылок
        isInitialized = (stat != null && obj != null && obj.Length >= 3 && obj[0] != null && obj[1] != null && obj[2] != null);
        if (!isInitialized) Debug.LogError("Not all objects initialized!");
    }



    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!view.IsMine) return; // Только хозяин объекта может атаковать

        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent<PhotonView>(out var targetView))
            {
                if (!targetView.IsMine) // Чтобы не атаковать себя
                {
                    Debug.Log($"Атакуем {collision.gameObject.name}");
                    targetView.RPC("TakeDamage", RpcTarget.All, stat._damage);
                }
            }
        }
    }


    internal void Move()
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
            // Проверка активных откатов перед применением движения
            HandleActiveRollbacks();
            
            // Регистрация нового состояния
            currentSequenceNumber++;
            lastInputTime = PhotonNetwork.Time;

            // Выполнение движения
            Move();
            RotateWeapon();
            fire.Fire();
            //stat.HP(obj[0]);
            
            // Сохранение текущего состояния в буфер
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
            
        // Применение активных откатов (если они затрагивают текущий объект)
        foreach (var rollback in activeRollbacks)
        {
            if (rollback.isActive)
            {
                // Здесь можно добавить логику для обработки конкретных эффектов отката
                // Например, замедление, блокировка некоторых действий и т.д.
                
                // Пример: временное снижение скорости при активном откате
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
            weaponRotation = obj[1].transform.rotation,
            timestamp = PhotonNetwork.Time
        });
    }

    private void RotateWeapon()
    {
        Vector3 mousePosition = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f;

        Vector3 directionToMouse = mousePosition - obj[2].transform.position;
        float targetAngle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg - 90f;

        obj[1].transform.rotation = Quaternion.Slerp(
            obj[1].transform.rotation,
            Quaternion.Euler(0f, 0f, targetAngle),
            rotationSpeed2 * Time.deltaTime
        );
    }

    private void ApplyNetworkInterpolation()
    {
        // Получение времени с учетом задержки сети (пинга)
        double interpolationTime = PhotonNetwork.Time - (PhotonNetwork.GetPing() * 0.001); // Преобразуем пинг из мс в секунды
        
        // Поиск двух состояний для интерполяции
        MovementState older = default;
        MovementState newer = default;
        bool foundStates = false;
        
        // Поиск состояний для интерполяции на основе времени
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
            // Телепорт, если объект слишком далеко (из-за лага)
            if (Vector3.Distance(obj[0].transform.position, correctPlayerPos) > teleportDistanceThreshold)
            {
                obj[0].transform.position = correctPlayerPos;
                obj[0].transform.rotation = correctPlayerRot;
                obj[1].transform.rotation = correctWeaponRot;
                return;
            }
            
            // Интерполяция к последнему известному состоянию
            obj[0].transform.position = Vector3.Lerp(obj[0].transform.position, correctPlayerPos, Time.deltaTime * smoothing);
            obj[0].transform.rotation = Quaternion.Slerp(obj[0].transform.rotation, correctPlayerRot, Time.deltaTime * smoothing);
            obj[1].transform.rotation = Quaternion.Slerp(obj[1].transform.rotation, correctWeaponRot, Time.deltaTime * smoothing);
            return;
        }
        
        if (foundStates)
        {
            // Расчет коэффициента интерполяции
            float t = (float)((interpolationTime - older.timestamp) / (newer.timestamp - older.timestamp));
            
            // Применение интерполяции
            obj[0].transform.position = Vector3.Lerp(older.position, newer.position, t);
            obj[0].transform.rotation = Quaternion.Slerp(older.rotation, newer.rotation, t);
            obj[1].transform.rotation = Quaternion.Slerp(older.weaponRotation, newer.weaponRotation, t);
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
        
        // Также можно отправить RPC для визуализации эффекта отката другим игрокам
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
        if (stream.IsWriting && photonView.IsMine)
        {
            // Отправка позиции, вращения и последовательного номера
            stream.SendNext(obj[0].transform.position);
            stream.SendNext(obj[0].transform.rotation);
            stream.SendNext(obj[1].transform.rotation);
            stream.SendNext(currentSequenceNumber);
            stream.SendNext(PhotonNetwork.Time); // Отправляем текущее серверное время
        }
        else
        {
            // Получение данных
            correctPlayerPos = (Vector3)stream.ReceiveNext();
            correctPlayerRot = (Quaternion)stream.ReceiveNext();
            correctWeaponRot = (Quaternion)stream.ReceiveNext();
            int receivedSequence = (int)stream.ReceiveNext();
            double timestamp = (double)stream.ReceiveNext();
            
            // Добавление полученного состояния в буфер для интерполяции
            if (movementBuffer.Count >= bufferSize)
            {
                movementBuffer.RemoveAt(0);
            }
            
            movementBuffer.Add(new MovementState
            {
                position = correctPlayerPos,
                rotation = correctPlayerRot,
                weaponRotation = correctWeaponRot,
                timestamp = timestamp
            });

            // Рассчитываем сглаживание на основе задержки
            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            smoothing = Mathf.Clamp(10f / (1f + lag), 5f, 20f);
        }
    }
}