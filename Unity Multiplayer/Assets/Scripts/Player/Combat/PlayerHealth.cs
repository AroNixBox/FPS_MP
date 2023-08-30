using System;
using System.Collections;
using SpectrumConsole;
using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private float respawnRadius = 5f;
    [SerializeField] private MonoBehaviour[] playerControlComponents;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float respawnTime;
    private Animator _animator;
    private int _currentHealth;
    private enum CurrentState
    {
        Alive, Dead
    }
    private static readonly int Died = Animator.StringToHash("Died");
    private CurrentState _state;
    

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(uint proposedDamageAmount, ulong playerID)
    {
        if (!IsOwner) return;
        if (!isAlive && _state != CurrentState.Alive) return;
        TakeDamageServerRpc(proposedDamageAmount, playerID);
    }

    [ServerRpc]
    void TakeDamageServerRpc(uint damageAmount, ulong playerID)
    {
        ApplyDamageClientRpc(damageAmount, playerID);
    }

    [ClientRpc]
    void ApplyDamageClientRpc(uint damageAmount, ulong playerID)
    {
        if (!IsOwner) return;
        _currentHealth -= (int)damageAmount;
        if (!isAlive && _state != CurrentState.Dead)
        {
            Die();
        }
        Debug.Log($"Player {playerID} has {_currentHealth} hp left");
    }
    [Command]
    private void Die()
    {
        if (!IsOwner) return;
        _state = CurrentState.Dead;
        Debug.LogWarning("You Died!");
        _animator.SetBool(Died, true);
        foreach (var component in playerControlComponents)
        {
            component.enabled = false;
        }
        StartCoroutine(Respawn());
    }
    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);
        _state = CurrentState.Alive;
        LoadComponents();
        _currentHealth = maxHealth;
    }
    private bool isAlive => _currentHealth > 0;

    private void LoadComponents()
    {
        Vector3 respawnPosition = GetRandomPositionAround(transform.position);
        while (transform.position != respawnPosition)
        {
            transform.position = respawnPosition;
        }
        _animator.SetBool(Died, false);
        foreach (var component in playerControlComponents)
        {
            component.enabled = true;
        }
    }

    private Vector3 GetRandomPositionAround(Vector3 center)
    {
        Vector2 randomCirclePoint = UnityEngine.Random.insideUnitCircle * respawnRadius;
        Vector3 randomPosition = center + new Vector3(randomCirclePoint.x, 0f, randomCirclePoint.y);

        return randomPosition;
    }
}
