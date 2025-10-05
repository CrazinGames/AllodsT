using Photon.Pun;
using Photon.Realtime;
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

    void IUpdatable.CustomFixedUpdate()
    {
        if (!photonView.IsMine) return;

        bool show = Input.GetKey(KeyCode.Tab);
        if (_scrollView.activeSelf != show)
        {
            ShowPlayerList(show);
        }
    }

    private void ShowPlayerList(bool show)
    {
        if (!photonView.IsMine) return;

        _scrollView.SetActive(show);
        UpdatePlayerList();
    }

    private void UpdatePlayerList()
    {
        if (!photonView.IsMine) return;

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
        if (!photonView.IsMine) return;

        if (_scrollView.activeSelf)
        {
            UpdatePlayerList();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!photonView.IsMine) return;

        if (_scrollView.activeSelf)
        {
            UpdatePlayerList();
        }
    }
}
