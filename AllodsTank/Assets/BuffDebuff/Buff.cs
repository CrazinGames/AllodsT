using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "Buff", menuName = "Scriptable Objects/Buff")]
public class Buff : ScriptableObject
{
    [SerializeField] internal string _name;
    [SerializeField] internal string _description;
    [SerializeField] internal float _duration;
    [SerializeField] internal float _delay;
    [SerializeField] internal GameObject _icon;
    [SerializeField] internal bool buffOrDebuff; //���� ����� �������, ������ �����
    [SerializeField] internal MonoScript script; //������ ��� ������ ����

}
