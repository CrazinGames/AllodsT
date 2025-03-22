using Photon.Pun;
using UnityEngine;

public class Spawn : MonoBehaviourPunCallbacks
{
    [SerializeField] private Transform[] _spawnPoints;

    [SerializeField] internal MountName _name;


    public void Awake() => SpawnPlayer();

    private void SpawnPlayer()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Spawned"))
        {
            return;
        }

        string NameMount = _name._mountName;

        Vector3 spawnPos = _spawnPoints[Random.Range(0, _spawnPoints.Length)].position;
        PhotonNetwork.Instantiate(NameMount, spawnPos, Quaternion.identity);

        PhotonNetwork.LocalPlayer.CustomProperties["Spawned"] = true;
    }
}
