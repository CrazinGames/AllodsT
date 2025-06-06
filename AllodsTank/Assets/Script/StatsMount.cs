using UnityEngine;
using System;
using TMPro;
using Unity.VisualScripting;

[CreateAssetMenu(fileName = "StatsMount", menuName = "Scriptable Objects/StatsMount")]
public class StatsMount : ScriptableObject
{
    
    [Header("������� �����")]
    [SerializeField] internal string _mountName;
    [SerializeField, Min(0)] internal float _hp = 100;
    [SerializeField, Min(0)] internal float _maxHp = 100;
    [SerializeField, Min(0)] internal float _damage = 10;
    [SerializeField, Min(0)] internal float _speed = 5;
    [SerializeField, Range(-360, 360)] internal float _speedRot = 90;


    public class MountStatsInstance
    {
        public string MountName { get; private set; }
        public float HP { get; private set; }
        public float MaxHP { get; private set; }
        public float Damage { get; private set; }
        public float Speed { get; private set; }
        public float SpeedRot { get; private set; }

        public MountStatsInstance(StatsMount baseStats)
        {
            MountName = baseStats._mountName;
            HP = baseStats._hp;
            MaxHP = baseStats._maxHp;
            Damage = baseStats._damage;
            Speed = baseStats._speed;
            SpeedRot = baseStats._speedRot;
        }

        public void TakeDamage(float damage, GameObject objToDisableIfDead = null)
        {
            HP = Mathf.Max(0, HP - damage);

            if (HP <= 0 && objToDisableIfDead != null)
            {
                objToDisableIfDead.SetActive(false);
            }
        }
    }

}