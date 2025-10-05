using System.Collections;
using TMPro;
using UnityEngine;

public class Battleground : MonoBehaviour
{
    [SerializeField] private TMP_Text kill;

    [SerializeField] private TMP_Text counting;
    private void Start() => StartCoroutine(Countdown(10));

    private IEnumerator Countdown(int score)
    {
        while (score > 0)
        {
            counting.text = score.ToString();
            score--;
            yield return new WaitForSeconds(1f);
        }

        counting.text = "ÍÀ×ÈÍÀÅÌ";
        yield return new WaitForSeconds(1.5f);
        HidePanel();
    }

    void HidePanel() => counting.transform.parent.gameObject.SetActive(false);


    public void TotalKill(float tkill) 
    {
        kill.text = tkill.ToString();
    }
}
