using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public void TakeDamage(float damageAmount)
    {
        Debug.Log($"Player has taken {damageAmount} damage");
    }
}
