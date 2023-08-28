using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    private int _currentHealth = 100;
    public void TakeDamage(uint damageAmount, ulong id)
    {
        // TODO Check if this line is correctly implemented. 
        // This function will be called from the server!
        if (!IsOwner) return;
        //Debug.Log($"Player {id} has taken {damageAmount} Damage!");
        // TODO Currently Playerhealth isnt getting shown to server
        _currentHealth -= (int)damageAmount;
        Debug.Log($"Player {id} has {_currentHealth} hp left");
    }
}
