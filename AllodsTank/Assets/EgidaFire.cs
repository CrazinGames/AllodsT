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
            //����� ����
            if (collision.gameObject.TryGetComponent<HP>(out var enemyHP))
            {
                // ��������� ������ ����� �� ��������
                var burnBuff = Resources.Load<Buff>("Burn");
                if (burnBuff != null)
                {
                    // �������� BuffManager ���� � ��������� ����
                    var enemyBuffManager = collision.gameObject.GetComponent<BuffManager>();
                    enemyBuffManager?.ApplyBuff(burnBuff);
                }
            }
            //����� ������
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