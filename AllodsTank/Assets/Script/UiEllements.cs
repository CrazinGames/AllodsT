using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static OneUpdate;

public class UiEllements : MonoBehaviourPunCallbacks, IUpdatable
{
    [SerializeField] private GameObject _scrollView;
    [SerializeField] private GameObject _playerNamePrefabA;
    [SerializeField] private GameObject _playerNamePrefabB;
    [SerializeField] private Transform _contentA;
    [SerializeField] private Transform _contentB;
    [SerializeField] private teamSelect team;

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

        counting.text = "НАЧИНАЕМ";
        yield return new WaitForSeconds(1.5f);
        HidePanel();
    }

    void HidePanel() => counting.transform.parent.gameObject.SetActive(false);


    void IUpdatable.CustomFixedUpdate()
    {
        if (Input.GetKey(KeyCode.Tab))
        {
            ShowPlayerList(true);
        }
        else
        {
            ShowPlayerList(false);
        }
    }


    private void ShowPlayerList(bool show)
    {
        _scrollView.SetActive(show);
        UpdatePlayerList();
    }

    private void UpdatePlayerList()
    {
        if (string.IsNullOrEmpty(team.selTeam))
        {
            return;
        }

        Transform content = team.selTeam == "A" ? _contentA : _contentB;
        GameObject playerPrefab = team.selTeam == "A" ? _playerNamePrefabA : _playerNamePrefabB;

        // Очистка текущего списка
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        // Добавление игроков
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject playerObject = Instantiate(playerPrefab, content);
            TMP_Text nameText = playerObject.GetComponentInChildren<TMP_Text>();

            if (nameText != null)
            {
                nameText.text = player.NickName;
            }
        }

        // Обновление расположения элементов
        LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {

        if (_scrollView.activeSelf)
        {
            UpdatePlayerList();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (_scrollView.activeSelf)
        {
            UpdatePlayerList();
        }
    }
}
