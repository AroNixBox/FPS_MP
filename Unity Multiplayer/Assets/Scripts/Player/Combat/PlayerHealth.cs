using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private float respawnRadius = 5f;
    [SerializeField] private MonoBehaviour[] playerControlComponents;
    [SerializeField] private float respawnTime;
    private Animator _animator;
    private CharacterController _controller;
    private CapsuleCollider _playerCollider;
    [SerializeField] private int maxHealth = 100;
    private int _currentHealth;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
        _playerCollider = GetComponent<CapsuleCollider>();
    }

    private void Start()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(uint proposedDamageAmount, ulong playerID)
    {
        if (!IsOwner) return;
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
        Debug.Log($"Player {playerID} has {_currentHealth} hp left");
        UpdateHealthClientRpc(_currentHealth); // Informiert die Clients über den neuen Gesundheitswert
    }

    [ClientRpc]
    void UpdateHealthClientRpc(int newHealth)
    {
        _currentHealth = newHealth;
        
        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (!IsOwner) return;
        Debug.LogWarning("You Died!");
        _animator.SetBool("Died", true);
        foreach (var component in playerControlComponents)
        {
            component.enabled = false;
        }

        _controller.enabled = false;
        _playerCollider.enabled = false;

        StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);
        LoadComponents();
        _currentHealth = maxHealth;
    }
    
    private void LoadComponents()
    {
        Vector3 respawnPosition = GetRandomPositionAround(transform.position);
        transform.position = respawnPosition;
        _animator.SetBool("Died", false);
        foreach (var component in playerControlComponents)
        {
            component.enabled = true;
        }

        _controller.enabled = true;
        _playerCollider.enabled = true;
    }

    private Vector3 GetRandomPositionAround(Vector3 center)
    {
        Vector2 randomCirclePoint = UnityEngine.Random.insideUnitCircle * respawnRadius;
        Vector3 randomPosition = center + new Vector3(randomCirclePoint.x, 0f, randomCirclePoint.y);

        return randomPosition;
    }
}
