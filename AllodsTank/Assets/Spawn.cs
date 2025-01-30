using Photon.Pun;
using System.IO;
using UnityEngine;

public class Spawn : MonoBehaviourPunCallbacks
{
    public GameObject SpawnPlayer;
    public float minX, minY, maxX, maxY;
    [SerializeField] private PhotonView view;

    public void Awake()
    {

            Vector3 pos2 = new(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY),
            -1
            );

            PhotonNetwork.Instantiate(Path.Combine(SpawnPlayer.name), pos2, Quaternion.identity, 0);
    }
}