using System.Collections;
using System.Collections.Generic;
using SpectrumConsole;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace SpectrumConsole
{
    public class SpawnPlayerLocalMultiplayer : NetworkBehaviour
    {
        [SerializeField] private Transform playerPrefab;
        [SerializeField] private Transform playerSpawnPosition;
        [Command]
        private void SpawnPlayerRequest()
        {
            Transform playerTransform = Instantiate(playerPrefab, playerSpawnPosition.position, playerSpawnPosition.rotation);
            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId, true);
        }
    }
}

