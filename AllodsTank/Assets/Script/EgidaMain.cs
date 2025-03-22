using Photon.Pun;
using UnityEngine;
using static OneUpdate;

public class EgidaMain : MonoBehaviour, OneUpdate.IUpdatable
{
    [SerializeField] private GameObject[] obj;
    [SerializeField] private float rotationSpeed2 = 10f;
    [SerializeField] private StatsMount stat;
    [SerializeField] private EgidaFire fire;
    [SerializeField] private CameraMove Cam;
    [SerializeField] private PhotonView view;

    private OneUpdate oneUpdate;

    private void Awake()
    {
        view = GetComponent<PhotonView>();
    }

    private void Start()
    {
        if (view.IsMine)
        {
            Cam = Camera.main?.GetComponent<CameraMove>();
            if (Cam == null)
            {
                Debug.LogError("CameraMove не найден на основном Camera.");
            }

            oneUpdate = FindAnyObjectByType<OneUpdate>();
            if (oneUpdate == null)
            {
                Debug.LogError("OneUpdate не найден на сцене.");
            }

            if (this is OneUpdate.IUpdatable updatable)
            {
                oneUpdate?.RegisterUpdatable(updatable);
                if (oneUpdate == null)
                {
                    Debug.LogError("Не удалось зарегистрировать объект в OneUpdate.");
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (view.IsMine && oneUpdate != null && this is OneUpdate.IUpdatable updatable)
        {
            oneUpdate.UnregisterUpdatable(updatable);
        }
    }

    void IUpdatable.CustomFixedUpdate()
    {
        if (view.IsMine)
        {
            MainObj();
            Obj();
            fire.Fire();
            Cam.camMove(false);
        }
    }

    private void MainObj()
    {
        if (stat == null || obj == null || obj.Length == 0 || obj[0] == null)
        {
            Debug.LogError("Stat или obj не инициализированы.");
            return;
        }

        Vector3 movement = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            movement = stat._speed * Time.deltaTime * obj[0].transform.up;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            movement = stat._speed * Time.deltaTime * -obj[0].transform.up;
        }

        if (Input.GetKey(KeyCode.A))
        {
            obj[0].transform.Rotate(Vector3.forward, stat._speedRot * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            obj[0].transform.Rotate(Vector3.forward, -stat._speedRot * Time.deltaTime);
        }

        if (movement != Vector3.zero)
        {
            obj[0].transform.position += movement;
        }
    }

    private void Obj()
    {
        if (Camera.main == null || obj == null || obj.Length < 3 || obj[2] == null)
        {
            return;
        }

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f;

        Vector3 directionToMouse = mousePosition - obj[2].transform.position;
        float targetAngle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg - 90f;

        obj[1].transform.rotation = Quaternion.Slerp(
            obj[1].transform.rotation,
            Quaternion.Euler(0f, 0f, targetAngle),
            rotationSpeed2 * Time.deltaTime
        );
    }
}