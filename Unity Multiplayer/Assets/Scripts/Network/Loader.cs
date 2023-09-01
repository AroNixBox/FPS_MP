using System;
using System.Collections.Generic;
using SpectrumConsole;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class Loader : NetworkBehaviour
{
    [SerializeField] private Transform playerPrefab;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventComplete;
        }
    }

    private void SceneManager_OnLoadEventComplete(string scenename, LoadSceneMode loadscenemode, List<ulong> clientscompleted, List<ulong> clientstimedout)
    {
        foreach (var clientID in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Transform playerTransform = Instantiate(playerPrefab);
            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, true);
        }
    }
    [Command]
    public void ChangeScene()
    {
        if(!IsServer) return;
        
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }
}