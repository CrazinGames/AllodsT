using Photon.Pun;
using UnityEngine;

public class NetworkMovement : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] private float interpolationBackTime = 0.1f;
    [SerializeField] private float smoothing = 10f; // Параметр для сглаживания движения
    
    private Vector3 correctPlayerPos;
    private Quaternion correctPlayerRot;
    private Vector2 correctPlayerVelocity;
    
    private Rigidbody2D rb;
    
    // Минимальный порог расстояния для обновления позиции
    private const float MIN_POSITION_DELTA = 0.05f;
    private const float MIN_ROTATION_DELTA = 1.0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Инициализация начальных значений
        correctPlayerPos = transform.position;
        correctPlayerRot = transform.rotation;
        
        if (rb != null)
            correctPlayerVelocity = rb.linearVelocity;
    }
    
    private void FixedUpdate()
    {
        if (!photonView.IsMine)
        {
            // Плавная интерполяция для удаленных игроков
            SmoothMovement();
        }
    }
    
    private void SmoothMovement()
    {
        // Плавно перемещаем объект к правильной позиции
        transform.position = Vector3.Lerp(transform.position, correctPlayerPos, Time.fixedDeltaTime * smoothing);
        
        // Плавный поворот к правильной ротации
        transform.rotation = Quaternion.Slerp(transform.rotation, correctPlayerRot, Time.fixedDeltaTime * smoothing);
        
        // Если есть Rigidbody2D, устанавливаем правильную скорость
        if (rb != null)
        {
            rb.linearVelocity = correctPlayerVelocity;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Отправляем данные только если мы владелец объекта
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            
            if (rb != null)
                stream.SendNext(rb.linearVelocity);
            else
                stream.SendNext(Vector2.zero);
        }
        else
        {
            // Получаем новую корректную позицию
            Vector3 newPosition = (Vector3)stream.ReceiveNext();
            Quaternion newRotation = (Quaternion)stream.ReceiveNext();
            Vector2 newVelocity = (Vector2)stream.ReceiveNext();
            
            // Проверяем, достаточно ли значительное изменение для обновления
            float positionDelta = Vector3.Distance(correctPlayerPos, newPosition);
            float rotationDelta = Quaternion.Angle(correctPlayerRot, newRotation);
            
            // Обновляем только если изменения значительны или прошло много времени с последнего обновления
            if (positionDelta > MIN_POSITION_DELTA || rotationDelta > MIN_ROTATION_DELTA)
            {
                correctPlayerPos = newPosition;
                correctPlayerRot = newRotation;
                correctPlayerVelocity = newVelocity;
                
                // Если объект слишком далеко, немедленно перемещаем его
                if (positionDelta > 5f)
                {
                    transform.position = correctPlayerPos;
                    transform.rotation = correctPlayerRot;
                }
            }
        }
    }
    
    // Немедленное обновление позиции при важных событиях
    public void ForceUpdatePosition(Vector3 position, Quaternion rotation)
    {
        if (!photonView.IsMine)
        {
            transform.position = position;
            transform.rotation = rotation;
            correctPlayerPos = position;
            correctPlayerRot = rotation;
        }
    }
    
    // Метод для обработки коллизий
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (photonView.IsMine)
        {
            // Отправляем всем другим клиентам нашу точную позицию при коллизии
            photonView.RPC("SyncPositionOnCollision", RpcTarget.Others, transform.position, transform.rotation);
        }
    }
    
    [PunRPC]
    private void SyncPositionOnCollision(Vector3 position, Quaternion rotation)
    {
        // Только если мы не владелец, обновляем позицию
        if (!photonView.IsMine)
        {
            ForceUpdatePosition(position, rotation);
        }
    }
}
