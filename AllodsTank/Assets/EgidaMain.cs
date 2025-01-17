using UnityEngine;

public class EgidaMain : MonoBehaviour
{
    [SerializeField] private GameObject obj;
    [SerializeField] private GameObject obj2;
    [SerializeField] private GameObject Center;

    [SerializeField] private float rotationSpeed2 = 10f;

    [SerializeField] private StatsMount stat;
    [SerializeField] private EgidaFire fire;


    private void FixedUpdate()
    {
        MainObj();
        Obj();
        fire.Fire();
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

        // Плавное вращение к позиции мыши
        obj2.transform.rotation = Quaternion.Slerp(
            obj2.transform.rotation,
            Quaternion.Euler(0f, 0f, targetAngle),
            rotationSpeed2 * Time.deltaTime
        );
    }
}