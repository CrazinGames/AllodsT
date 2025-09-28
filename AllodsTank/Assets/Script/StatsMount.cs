using UnityEngine;
using System;
using TMPro;
using Unity.VisualScripting;

[CreateAssetMenu(fileName = "StatsMount", menuName = "Scriptable Objects/StatsMount")]
public class StatsMount : ScriptableObject
{
    
    [Header("Базовые статы")]
    [SerializeField] internal string _mountName;
    [SerializeField, Min(0)] internal float _hp = 100;
    [SerializeField, Min(0)] internal float _maxHp = 100;
    [SerializeField, Min(0)] internal float _damage = 10;
    [SerializeField, Min(0)] internal float _speed = 5;
    [SerializeField, Range(-360, 360)] internal float _speedRot = 90;
    [SerializeField, Range(-360, 360)] internal float _speedRot2 = 10f;


    public class MountStatsInstance
    {
        public string MountName { get; private set; }
        public float HP { get; internal set; }
        public float MaxHP { get; private set; }
        public float Damage { get; private set; }
        public float Speed { get; private set; }
        public float SpeedRot { get; private set; }
        public float SpeedRot2 { get; private set; }

        public MountStatsInstance(StatsMount baseStats)
        {
            MountName = baseStats._mountName;
            HP = baseStats._hp;
            MaxHP = baseStats._maxHp;
            Damage = baseStats._damage;
            Speed = baseStats._speed;
            SpeedRot = baseStats._speedRot;
            SpeedRot2 = baseStats._speedRot2;
        }

    }

}