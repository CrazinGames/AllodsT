using UnityEngine;

[CreateAssetMenu(fileName = "MountName", menuName = "Scriptable Objects/MountName")]
public class MountName : ScriptableObject
{
   [SerializeField] internal string _mountName = null;
}
