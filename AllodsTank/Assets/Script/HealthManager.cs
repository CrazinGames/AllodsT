using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using System.Collections;
using System.Collections.Generic;

public class HealthManager : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private Image healthSlider;
    [SerializeField] private GameObject damageEffect;
    [SerializeField] private StatsMount statsMount;
    [SerializeField] private float damageTickRate = 0.1f; // Минимальное время между синхронизациями урона
    [SerializeField] private float syncInterval = 1.0f; // Интервал полной синхронизации здоровья
    
    private float currentHealth;
    private PhotonView photonView;
    private bool isDead;
    
    // Переменные для оптимизации сети
    private float lastDamageTime;
    private float lastHealthSyncTime;
    private float syncedHealth;
    
    // Пул эффектов урона
    private ObjectPool damageEffectPool;
    
    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        
        // Инициализация значения здоровья
        float startHP = statsMount != null ? statsMount._hp : maxHealth;
        currentHealth = startHP;
        syncedHealth = startHP;
        
        // Создаем пул эффектов урона
        if (damageEffect != null)
        {
            damageEffectPool = new ObjectPool(damageEffect, 5);
        }
        
        UpdateUI();
    }


    
    private void Start()
    {
        // Начинаем периодически синхронизировать здоровье
        if (photonView.IsMine)
        {
            StartCoroutine(SyncHealthPeriodically());
        }
    }
    
    // Периодическая синхронизация здоровья для всех клиентов
    private IEnumerator SyncHealthPeriodically()
    {
        WaitForSeconds wait = new WaitForSeconds(syncInterval);
        
        while (true)
        {
            yield return wait;
            
            if (Mathf.Abs(currentHealth - syncedHealth) > 0.01f)
            {
                syncedHealth = currentHealth;
                lastHealthSyncTime = Time.time;
                
                // Отправляем всем клиентам актуальное значение здоровья
                photonView.RPC("SyncHealth", RpcTarget.Others, currentHealth, isDead);
            }
        }
    }
    
    [PunRPC]
    private void SyncHealth(float health, bool dead)
    {
        // Синхронизация значений только если они существенно отличаются
        if (Mathf.Abs(currentHealth - health) > 0.1f)
        {
            currentHealth = health;
            isDead = dead;
            UpdateUI();
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // Оптимизируем, отправляя только существенные изменения
        if (stream.IsWriting)
        {
            // Отправляем только если прошло достаточно времени или значения значительно изменились
            if (Time.time - lastHealthSyncTime > syncInterval || 
                Mathf.Abs(currentHealth - syncedHealth) > maxHealth * 0.05f)
            {
                stream.SendNext(currentHealth);
                stream.SendNext(isDead);
                syncedHealth = currentHealth;
                lastHealthSyncTime = Time.time;
            }
            else
            {
                // Отправляем текущие значения для поддержания синхронизации
                stream.SendNext(syncedHealth); 
                stream.SendNext(isDead);
            }
        }
        else
        {
            float receivedHealth = (float)stream.ReceiveNext();
            bool receivedIsDead = (bool)stream.ReceiveNext();
            
            // Применяем изменения только если изменения значительные
            if (Mathf.Abs(currentHealth - receivedHealth) > 0.5f || isDead != receivedIsDead)
            {
                currentHealth = receivedHealth;
                isDead = receivedIsDead;
                UpdateUI();
            }
        }
    }

    
    private void HandleDamageEvent(float damage)
    {
        // Воспроизводим эффект урона
        if (damageEffectPool != null && !isDead)
        {
            damageEffectPool.SpawnFromPool(transform.position, Quaternion.identity);
        }
    }
    
    private void HandleHealEvent(float healAmount)
    {
        // Можно добавить эффект лечения
    }

    [PunRPC]
    public void TakeDamage(float damage, string attackerID)
    {
        if (!photonView.IsMine || isDead)
            return;
            
        // Предотвращаем слишком частые вызовы урона
        if (Time.time - lastDamageTime < damageTickRate)
        {
            return;
        }
        
        lastDamageTime = Time.time;
        float previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        // Создаем эффект урона из пула
        if (damageEffectPool != null)
        {
            damageEffectPool.SpawnFromPool(transform.position, Quaternion.identity);
        }


        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            photonView.RPC("OnPlayerDeath", RpcTarget.All, attackerID);
        }
        
        // Если здоровье сильно изменилось, синхронизируем немедленно
        if (Mathf.Abs(previousHealth - currentHealth) > maxHealth * 0.1f)
        {
            syncedHealth = currentHealth;
            lastHealthSyncTime = Time.time;
            photonView.RPC("SyncHealth", RpcTarget.Others, currentHealth, isDead);
        }

        UpdateUI();
    }

    [PunRPC]
 
    
    private IEnumerator DelayedDeactivate()
    {
        // Задержка перед деактивацией для воспроизведения эффектов
        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
    }

    private void UpdateUI()
    {
        if (healthSlider != null)
        {
            float maxHealthValue = statsMount != null ? statsMount._hp : maxHealth;
            healthSlider.fillAmount = currentHealth / maxHealthValue;
        }
    }

    public void Respawn()
    {
        if (!photonView.IsMine)
            return;

        isDead = false;
        currentHealth = statsMount != null ? statsMount._hp : maxHealth;
        syncedHealth = currentHealth;
        gameObject.SetActive(true);
        UpdateUI();

        
        // Форсируем синхронизацию при респауне
        photonView.RPC("SyncHealth", RpcTarget.Others, currentHealth, isDead);
    }
    
    // Вспомогательный класс для управления пулом объектов
    private class ObjectPool
    {
        private GameObject prefab;
        private Queue<GameObject> pool;
        
        public ObjectPool(GameObject prefab, int initialSize)
        {
            this.prefab = prefab;
            pool = new Queue<GameObject>();
            
            for (int i = 0; i < initialSize; i++)
            {
                GameObject obj = Object.Instantiate(prefab);
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
        }
        
        public GameObject SpawnFromPool(Vector3 position, Quaternion rotation)
        {
            GameObject obj;
            
            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else
            {
                obj = Object.Instantiate(prefab);
            }
            
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            
            // Автоматический возврат в пул
            MonoBehaviour mb = obj.GetComponent<MonoBehaviour>();
            if (mb != null)
            {
                mb.StartCoroutine(ReturnToPoolAfterDelay(obj, 1.5f));
            }
            
            return obj;
        }
        
        private IEnumerator ReturnToPoolAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool(obj);
        }
        
        public void ReturnToPool(GameObject obj)
        {
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }
}
