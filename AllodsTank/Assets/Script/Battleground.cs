using TMPro;
using UnityEngine;

public class Battleground : MonoBehaviour
{
    [SerializeField] private TMP_Text kill;

    public void TotalKill(float tkill) 
    {
        kill.text = tkill.ToString();
    }
}
