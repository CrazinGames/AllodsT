using Photon.Pun;
using UnityEngine;

public class EgidaMain : MonoBehaviourPunCallbacks, IPunObservable, OneUpdate.IUpdatable
{
    [SerializeField] private GameObject[] obj;
    [SerializeField] private float rotationSpeed2 = 10f;
    [SerializeField] private StatsMount stat;
    [SerializeField] private EgidaFire fire;
    [SerializeField] private CameraMove Cam;
    [SerializeField] private float interpolationBackTime = 0.1f;
    [SerializeField] private float smoothing = 10f;

    private OneUpdate oneUpdate;
    private Rigidbody2D rb;
    private Vector3 correctPlayerPos;
    private Quaternion correctPlayerRot;
    private Vector2 correctPlayerVelocity;

    private const float MIN_POSITION_DELTA = 0.05f;
    private const float MIN_ROTATION_DELTA = 1.0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        correctPlayerPos = transform.position;
        correctPlayerRot = transform.rotation;
        correctPlayerVelocity = rb != null ? rb.linearVelocity : Vector2.zero;
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            Cam = Camera.main?.GetComponent<CameraMove>();
            if (Cam == null)
                Debug.LogError("CameraMove не найден на основном Camera.");

            oneUpdate = FindAnyObjectByType<OneUpdate>();
            if (oneUpdate == null)
                Debug.LogError("OneUpdate не найден на сцене.");

            oneUpdate?.RegisterUpdatable(this);
        }
    }

    private void OnDestroy()
    {
        if (photonView.IsMine && oneUpdate != null)
            oneUpdate.UnregisterUpdatable(this);
    }

    public void CustomFixedUpdate()
    {
        if (photonView.IsMine)
        {
            MainObj();
            Obj();
            fire.Fire();
            Cam?.camMove(false);
        }
        else
        {
            SmoothMovement();
        }
    }

    private void MainObj()
    {
        if (stat == null || obj == null || obj.Length == 0 || obj[0] == null)
            return;

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

    private void Obj()
    {
        if (Camera.main == null || obj == null || obj.Length < 3 || obj[2] == null)
            return;

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

    private void SmoothMovement()
    {
        transform.position = Vector3.Lerp(transform.position, correctPlayerPos, Time.fixedDeltaTime * smoothing);
        transform.rotation = Quaternion.Slerp(transform.rotation, correctPlayerRot, Time.fixedDeltaTime * smoothing);

        if (rb != null)
            rb.linearVelocity = correctPlayerVelocity;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(rb != null ? rb.linearVelocity : Vector2.zero);
        }
        else
        {
            correctPlayerPos = (Vector3)stream.ReceiveNext();
            correctPlayerRot = (Quaternion)stream.ReceiveNext();
            correctPlayerVelocity = (Vector2)stream.ReceiveNext();
        }
    }
}
