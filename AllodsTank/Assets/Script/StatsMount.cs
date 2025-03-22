using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "StatsMount", menuName = "Scriptable Objects/StatsMount")]
public class StatsMount : ScriptableObject
{

    [SerializeField] internal string _mountName;
    [SerializeField] internal float _hp;
    [SerializeField] internal float _damage;
    [SerializeField] internal float _speed;
    [SerializeField] internal float _speedRot;


}
