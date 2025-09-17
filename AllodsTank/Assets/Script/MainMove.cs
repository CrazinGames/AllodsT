using UnityEngine;

public class MainMove : MonoBehaviour
{
    [SerializeField] private GameObject[] obj;
    [SerializeField] private StatsMount stat;

    [SerializeField] private Camera mainCam;
    private bool isInitialized = false;

    private void Start()
    {
        // Автоматическая инициализация 
        if (obj != null && obj[0] != null && stat != null)
        {
            mainCam = Camera.main;
            isInitialized = true;

            if (mainCam == null)
                Debug.LogError("Main camera not found!");
        }
    }

    // Альтернативный метод инициализации если нужно настроить из кода
    public void Initialize(GameObject[] objects, StatsMount stats)
    {
        obj = objects;
        stat = stats;
        mainCam = Camera.main;
        isInitialized = true;

        if (mainCam == null)
            Debug.LogError("Main camera not found!");
    }

    // Хуйня для передивжения
    public void Move()
    {
        if (!isInitialized) return;

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

    // Крутилка
    public void RotateWeapon()
    {
        if (!isInitialized || mainCam == null) return;

        Vector3 mousePosition = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f;

        Vector3 directionToMouse = mousePosition - obj[2].transform.position;
        float targetAngle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg - 90f;

        obj[1].transform.rotation = Quaternion.Slerp(
            obj[1].transform.rotation,
            Quaternion.Euler(0f, 0f, targetAngle),
            stat._speedRot2 * Time.deltaTime
        );
    }
}