using UnityEngine;

public class EgidaFire : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Collider2D collision;
    //[SerializeField] private float damage = 0.5f;

    private void FireAnim(bool _bool)
    {
        _animator.SetBool("Fire", _bool);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            //Вызов бафа
            if (collision.gameObject.TryGetComponent<HP>(out var enemyHP))
            {
                // Загружаем объект баффа из ресурсов
                var burnBuff = Resources.Load<Buff>("Burn");
                if (burnBuff != null)
                {
                    // Получаем BuffManager цели и применяем бафф
                    var enemyBuffManager = collision.gameObject.GetComponent<BuffManager>();
                    enemyBuffManager?.ApplyBuff(burnBuff);
                }
            }
            //Конец вызова
        }
    }

    internal void Fire()
    {
        if (Input.GetMouseButton(0))
        {
            collision.enabled = true;
            FireAnim(true);
        }
        else
        {
            FireAnim(false);
            collision.enabled = false;
        }
    }
}