using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    private int x = 100;
    public void TakeDamage(uint damageAmount, ulong id)
    {
        // TODO Check if this line is correctly implemented. 
        // This function will be called from the server!
        if (!IsOwner) return;
        //Debug.Log($"Player {id} has taken {damageAmount} Damage!");
        x -= (int)damageAmount;
        Debug.Log($"Player {id} has {x} hp left");
    }
}
