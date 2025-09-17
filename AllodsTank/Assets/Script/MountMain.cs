using Photon.Pun;
using UnityEngine;
using static OneUpdate;

public class MountMain : MonoBehaviourPunCallbacks, IUpdatable
{
    [Header("References")]
    [SerializeField] private StatsMount stat;
    [SerializeField] private MainMove move;
    [SerializeField] private PhotonView view;

    [SerializeField] private bool tower;

    [SerializeField] private MonoBehaviour _attack;

    private CameraMove cam;
    private bool isInitialized = false;

    private void Awake()
    {
        if (!photonView.IsMine) return;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            cam = mainCam.GetComponent<CameraMove>();
            if (cam == null)
                Debug.LogError("CameraMove not found on main camera!");
        }
        else
        {
            Debug.LogError("Main camera not found!");
        }

        // Проверяем инициализацию
        isInitialized = (stat != null && move != null && view != null);
        if (!isInitialized)
        {
            Debug.LogError("Not all objects initialized in EgidaMain!");
            return;
        }

        // Регистрируем в OneUpdate
        var oneUpdate = FindAnyObjectByType<OneUpdate>();
        if (oneUpdate == null)
            Debug.LogError("OneUpdate not found in scene!");
        else
            oneUpdate.RegisterUpdatable(this);
    }

    private void OnDestroy()
    {
        if (photonView.IsMine)
        {
            var oneUpdate = FindAnyObjectByType<OneUpdate>();
            if (oneUpdate != null)
                oneUpdate.UnregisterUpdatable(this);
        }
    }

    //Мейн апдейт, пихаем все методы сюда 
    void IUpdatable.CustomFixedUpdate()
    {
        if (!isInitialized || !photonView.IsMine) return;

        move.Move();

        if (tower == true)
            move.RotateWeapon();

        ExecuteTargetMethod("Fire"); // Вызов этого унив. метода 

        if (cam != null)
            cam.camMove(false);
    }

    // Универсальный метод для атаки
    void ExecuteTargetMethod(string nameMethod)
    {
        if (_attack != null && !string.IsNullOrEmpty(nameMethod))
        {
            _attack.SendMessage(nameMethod,
                SendMessageOptions.DontRequireReceiver);
        }
    }
}