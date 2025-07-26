using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiEllements : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject _scrollView;
    [SerializeField] private GameObject _playerNamePrefabA;
    [SerializeField] private GameObject _playerNamePrefabB;
    [SerializeField] private Transform _contentA;
    [SerializeField] private Transform _contentB;
    [SerializeField] private teamSelect team;



    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ShowPlayerList(true);
        }

        if (Input.GetKeyUp(KeyCode.Tab))
        {
            ShowPlayerList(false);
        }
    }

    private void ShowPlayerList(bool show)
    {
        _scrollView.SetActive(show);
        UpdatePlayerList();
        _scrollView.SetActive(show);
        UpdatePlayerList();

    }

    private void UpdatePlayerList()
    {
        if (string.IsNullOrEmpty(team.selTeam))
        {
            Debug.LogWarning("Команда не выбрана!");
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
