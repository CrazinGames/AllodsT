using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class OnlineManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_InputField Create;
    [SerializeField] private TMP_InputField Join;

    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void CreateRoom()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = 4;
            PhotonNetwork.CreateRoom(Create.text, roomOptions);
            Debug.Log("Room creation requested.");
        }
        else
        {
            Debug.LogError("Cannot create room. Client is not ready or not connected to Master Server.");
        }
    }

    public void JoinRoom()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinRoom(Join.text);
            Debug.Log("Room join requested.");
        }
        else
        {
            Debug.LogError("Cannot join room. Client is not ready or not connected to Master Server.");
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server. Ready to create/join rooms.");
        PhotonNetwork.JoinLobby(); // Рекомендуется войти в лобби для работы с комнатами
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby.");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Successfully joined room. Loading game scene...");
        PhotonNetwork.LoadLevel("Game");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Room creation failed: {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Room join failed: {message}");
    }
}
