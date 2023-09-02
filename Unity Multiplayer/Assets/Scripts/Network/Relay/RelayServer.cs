using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayServer : MonoBehaviour
{
    public static RelayServer Instance;
    [Header("Max. Connections (HOST EXCLUDED)")]
    [SerializeField] [Range(1, 5)] private int maxConnections;

    private void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; 
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this) 
        { 
            Destroy(gameObject);
        }
    }
    public async Task<string> CreateRelay()
   {
       //Max Players + Host (3 Means 3 Players + Host = 4)
       try
       {
           Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
           string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
           Debug.Log(joinCode);

           RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
           NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
           NetworkManager.Singleton.StartHost();
           return joinCode;
       }
       catch (RelayServiceException e)
       {
           Debug.Log(e);
           return null;
       }

   }
    public async void JoinRelay(string joinCode)
   {
       try
       {
           Debug.Log("Joining Relay with " + joinCode);
           JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
           
           RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
           NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

           NetworkManager.Singleton.StartClient();
       }
       catch (RelayServiceException e)
       {    
           Debug.Log(e);
       }
   }
}
