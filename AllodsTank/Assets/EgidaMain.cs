using Photon.Pun;
using UnityEngine;
using static OneUpdate;

public class EgidaMain : MonoBehaviour, IUpdatable, IPunObservable
{
    [SerializeField] private GameObject obj;
    [SerializeField] private GameObject obj2;
    [SerializeField] private GameObject Center;

    [SerializeField] private float rotationSpeed2 = 10f;

    [SerializeField] private StatsMount stat;
    [SerializeField] private EgidaFire fire;
    [SerializeField] private CameraMove Cam;
    [SerializeField] private PhotonView view;

    private OneUpdate oneUpdate; // Возвращаем oneUpdate

    private Vector3 networkPosition;
    private Quaternion networkRotation;

    private void Start()
    {
        if (view.IsMine)
        {
            Cam = Camera.main.GetComponent<CameraMove>();
            oneUpdate = FindAnyObjectByType<OneUpdate>(); // Возвращаем строку
            oneUpdate?.RefreshUpdatableScripts(); // Регистрируем в OneUpdate
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
        else
        {
            // Интерполяция движения для других игроков
            obj.transform.position = Vector3.Lerp(obj.transform.position, networkPosition, Time.deltaTime * 10f);
            obj.transform.rotation = Quaternion.Lerp(obj.transform.rotation, networkRotation, Time.deltaTime * 10f);
        }
    }

    private void MainObj()
    {
        Vector3 movement = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            movement = stat._speed * Time.deltaTime * obj.transform.up;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            movement = stat._speed * Time.deltaTime * -obj.transform.up;
        }

        if (Input.GetKey(KeyCode.A))
        {
            obj.transform.Rotate(Vector3.forward, stat._speedRot * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            obj.transform.Rotate(Vector3.forward, -stat._speedRot * Time.deltaTime);
        }

        if (movement != Vector3.zero)
        {
            obj.transform.position += movement;
        }
    }

    private void Obj()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f;

        Vector3 directionToMouse = mousePosition - Center.transform.position;
        float targetAngle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg - 90f;

        obj2.transform.rotation = Quaternion.Slerp(
            obj2.transform.rotation,
            Quaternion.Euler(0f, 0f, targetAngle),
            rotationSpeed2 * Time.deltaTime
        );
    }

    // Синхронизация через Photon
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) // Мы отправляем данные
        {
            stream.SendNext(obj.transform.position);
            stream.SendNext(obj.transform.rotation);
        }
        else // Получаем данные от других игроков
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
        }
    }
}
