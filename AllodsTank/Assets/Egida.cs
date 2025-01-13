using UnityEngine;

public class Egida : MonoBehaviour
{
    [Header("Object References")]
    public GameObject obj;
    public GameObject mainObj;
    public GameObject Center;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 50f;

    private void Update()
    {
        MainObj();
        Obj();
    }

    private void MainObj()
    {
        Vector3 movement = Vector3.zero;

        // Движение вперед/назад
        if (Input.GetKey(KeyCode.W))
        {
            movement = moveSpeed * Time.deltaTime * mainObj.transform.up;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            movement = moveSpeed * Time.deltaTime * -mainObj.transform.up;
        }

        // Поворот влево/вправо
        if (Input.GetKey(KeyCode.A))
        {
            mainObj.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            mainObj.transform.Rotate(Vector3.forward, -rotationSpeed * Time.deltaTime);
        }

        // Применение движения
        if (movement != Vector3.zero)
        {
            mainObj.transform.position += movement;
        }
    }

    private void Obj()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f;

        Vector3 directionToMouse = mousePosition - Center.transform.position;
        float targetAngle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg - 90f;

        // Плавное вращение к позиции мыши
        obj.transform.rotation = Quaternion.Slerp(
            obj.transform.rotation,
            Quaternion.Euler(0f, 0f, targetAngle),
            rotationSpeed * Time.deltaTime
        );
    }
}