using System.Collections;
using Unity.Netcode;
using UnityEngine;


public enum PlayerType
{
    None,
    TeamBlue,
    TeamRed
}
public class PlayerInfo : NetworkBehaviour
{
    public PlayerType thisPlayersTeam = PlayerType.None;
    IEnumerator Start()
    {
        // TODO This defines the team locally, find a way to Call this not from start, but rather via event from Loader Class
        yield return new WaitForSeconds(1f);
        if (IsOwner) 
            RequestPlayerTypeServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc]
    private void RequestPlayerTypeServerRpc(ulong clientId, ServerRpcParams rpcParams = default)
    {
        PlayerType typeForClient = Loader.Instance.GetPlayerTypeForClient(clientId); 
        UpdatePlayerTypeClientRpc(clientId, typeForClient);
        Debug.Log(clientId);
    }

    [ClientRpc]
    private void UpdatePlayerTypeClientRpc(ulong clientId, PlayerType type)
    {
        thisPlayersTeam = type;
    }

}
