using UnityEngine;

[CreateAssetMenu(fileName = "teamSelect", menuName = "Scriptable Objects/teamSelect")]
public class teamSelect : ScriptableObject
{
    [SerializeField] internal string selTeam = null;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(selTeam))
        {
            selTeam = Random.value < 0.5f ? "A" : "B";
        }
    }
}