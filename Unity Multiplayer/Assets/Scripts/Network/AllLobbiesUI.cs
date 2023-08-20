using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class AllLobbiesUI : MonoBehaviour
{
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Transform container;
    [SerializeField] private Transform lobbySingleTransform;
    

    private void RefreshButtonClick()
    {
        //LobbyManager.Instance.
    }

    private void UpdateLobbyList(List<Lobby> lobbyList)
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
        foreach (Lobby lobby in lobbyList)
        {
            //Transform lobbyListSingleUI = Instantiate(this.lobbySingleTransform.GetComponent<LobbyListSingleUI>());
            //lobbyListSingleUI.UpdateLobby(lobby);
        }
    }
}
