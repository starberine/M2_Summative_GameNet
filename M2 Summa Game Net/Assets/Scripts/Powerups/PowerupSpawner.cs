using UnityEngine;
using Photon.Pun;
using System.Collections;

public class PowerUpSpawner : MonoBehaviourPunCallbacks
{
    public Transform[] spawnPoints;
    public string[] powerUpPrefabs = { "Powerups/SpeedBoost", "Powerups/ExtraPoints" };
    public float spawnInterval = 15f;

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
            StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnRandomPowerUp();
        }
    }

    void SpawnRandomPowerUp()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        string prefabName = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)];

        PhotonNetwork.Instantiate(prefabName, spawnPoint.position, Quaternion.identity);
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
            StartCoroutine(SpawnRoutine());
    }
}
