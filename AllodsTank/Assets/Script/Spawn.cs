using Photon.Pun;
using UnityEngine;

public class Spawn : MonoBehaviourPunCallbacks
{
    [SerializeField] private Transform[] _spawnPointsA;
    [SerializeField] private Transform[] _spawnPointsB;

    [SerializeField] internal MountName _name;
    [SerializeField] private teamSelect team;

    public void Awake() => SpawnPlayer();

    private void SpawnPlayer()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Spawned"))
        {
            return;
        }

        string NameMount = _name._mountName;


        if (team.selTeam == "A")
        {
            Vector3 spawnPos = _spawnPointsA[Random.Range(0, _spawnPointsA.Length)].position;
            PhotonNetwork.Instantiate(NameMount, spawnPos, Quaternion.identity);
        }

        if (team.selTeam == "B")
        {
            Vector3 spawnPos = _spawnPointsB[Random.Range(0, _spawnPointsB.Length)].position;
            PhotonNetwork.Instantiate(NameMount, spawnPos, Quaternion.identity);
        }

        PhotonNetwork.LocalPlayer.CustomProperties["Spawned"] = true;
    }
}