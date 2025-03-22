using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiEllements : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject _scrollView;
    [SerializeField] private GameObject _playerNamePrefab;
    [SerializeField] private Transform _content;

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
    }

    private void UpdatePlayerList()
    {

        foreach (Transform child in _content)
        {
            Destroy(child.gameObject);
        }

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject playerObject = Instantiate(_playerNamePrefab, _content);
            TMP_Text Name = playerObject.GetComponentInChildren<TMP_Text>();

            if (Name != null)
            {
                Name.text = player.NickName;
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_content.GetComponent<RectTransform>());
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
