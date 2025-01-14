using UnityEngine;

public class EgidaMove : MonoBehaviour
{
    [Header("Object References")]
    [SerializeField] private GameObject obj;
    [SerializeField] private GameObject mainObj;
    [SerializeField] private GameObject Center;
    [SerializeField] private Animator _animator;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float rotationSpeed2 = 10f;

    private void FixedUpdate()
    {
        MainObj();
        Obj();
        FireAnim();
    }


    private void FireAnim()
    {
        _animator.SetBool("Fire", Input.GetKey(KeyCode.Mouse0));
    }

    private void MainObj()
    {
        Vector3 movement = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            movement = moveSpeed * Time.deltaTime * mainObj.transform.up;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            movement = moveSpeed * Time.deltaTime * -mainObj.transform.up;
        }

        if (Input.GetKey(KeyCode.A))
        {
            mainObj.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            mainObj.transform.Rotate(Vector3.forward, -rotationSpeed * Time.deltaTime);
        }

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
            rotationSpeed2 * Time.deltaTime
        );
    }
}