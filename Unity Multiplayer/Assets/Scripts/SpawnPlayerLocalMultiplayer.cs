using SpectrumConsole;
using Unity.Netcode;
using UnityEngine;

public class SpawnPlayerLocalMultiplayer : NetworkBehaviour
{
    // TODO Remove this, this is just for Blockout trying
    [SerializeField] private Transform playerPrefab;
    [SerializeField] private Transform[] playerSpawnPosition;
    [Command]
    private void SpawnHostRequest()
    {
        foreach (var clientID in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Transform thisPlayersSpawnPos = playerSpawnPosition[Random.Range(0, playerSpawnPosition.Length)];
            Transform playerTransform = Instantiate(playerPrefab, thisPlayersSpawnPos.position, thisPlayersSpawnPos.rotation);
            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, true);
        }
    }
}