using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField]
    private List<WeaponSO> weapons;
    private int _currentWeaponIndex = 0;

    private WeaponSO CurrentSelectedWeapon;
    private Dictionary<string, int> _allWeaponBulletsStorage = new Dictionary<string, int>();
    private int _currentBullets;
    [SerializeField] private Transform shootingStartingPos;
    
    //Properties will be overwritten by WeaponSO Properties
    private string _weaponName;
    private float _minTimeBetweenShots;
    private uint _bodyshotDamage;
    private uint _headshotDamage;
    private float _inaccuracyOnBulletSpam;
    private uint _maxBullets;
    private float _reloadTime;
    private uint _maxConsecutiveShots;
    private float _recoveryDelay;
    private float _recoveryRate;

    private bool _isReloading;
    private float _shootCooldown;
    //reference to Reloading Coroutine, so can cancel if switch gun
    private Coroutine _reloadingCoroutine;
    
    //The current Shots spammed one after another, get reduced with recoveryRate after recoveryDelay each second. The higher this is the more inaccurate you are
    //Limited by maxConsecutiveShots
    private float _consecutiveShots;
    //When did the last Shot happen (Time).
    private float _lastShotTime;

    private void Start()
    {
        if(!IsOwner)
            return;

        CurrentSelectedWeapon = weapons[_currentWeaponIndex];
        LoadCurrentWeaponProperties();
    }

    private void Update()
    {
        if (!IsOwner)
            return;
        
        //Recovery for Recoil
        if (Time.time - _lastShotTime > _recoveryDelay)
        {
            _consecutiveShots -= _recoveryRate * Time.deltaTime;
            _consecutiveShots = Mathf.Max(_consecutiveShots, 0);
        }
        
        RequestSwitchGunWithScroll();

        //ON Lmb hold shoot in firerate
        _shootCooldown -= Time.deltaTime;
        if (Input.GetMouseButton(0) && _shootCooldown <= 0) 
        {
            Shoot();
            _shootCooldown = _minTimeBetweenShots;
            // Für Bullet Spam Inaccuracy
            _consecutiveShots = Mathf.Min(_consecutiveShots + 1, _maxConsecutiveShots);
            _lastShotTime = Time.time;
        }
    }

    private void RequestSwitchGunWithScroll()
    {
        //Only 1 Weapon, cant switch Weapon
        if (weapons.Count <= 1)
            return;

        var previousWeaponIndex = _currentWeaponIndex;

        var scrollValue = Input.mouseScrollDelta.y;
        
        if (scrollValue > 0)
        {
            _currentWeaponIndex++;
            if (_currentWeaponIndex >= weapons.Count)
                _currentWeaponIndex = 0;
        }
        else if (scrollValue < 0)
        {
            _currentWeaponIndex--;
            if (_currentWeaponIndex < 0)
                _currentWeaponIndex = weapons.Count - 1;
        }
        
        //If I actually changed the gun
        if (previousWeaponIndex != _currentWeaponIndex)
        {
            SwitchWeapon();
        }
    }

    private void SwitchWeapon()
    {
        CurrentSelectedWeapon = weapons[_currentWeaponIndex];
        //If is, Stop Reloading
        if (_isReloading && _reloadingCoroutine != null)
        {
            StopCoroutine(_reloadingCoroutine);
            _isReloading = false;
        }
        //Before overwriting, change dictionary entry: Bullets for this gun to my current bullets, so if I switch back, value gets load from dictionary
        _allWeaponBulletsStorage[_weaponName] = _currentBullets;
        LoadCurrentWeaponProperties();
    }

    private void LoadCurrentWeaponProperties()
    {
        if (CurrentSelectedWeapon != null)
        {
            _weaponName = CurrentSelectedWeapon.weaponName;
            _minTimeBetweenShots = CurrentSelectedWeapon.minTimeBetweenShots;
            _bodyshotDamage = CurrentSelectedWeapon.bodyshotDamage;
            _headshotDamage = CurrentSelectedWeapon.headshotDamage;
            _inaccuracyOnBulletSpam = CurrentSelectedWeapon.inaccuracyOnBulletSpam;
            _maxBullets = CurrentSelectedWeapon.maxBullets;
            _reloadTime = CurrentSelectedWeapon.reloadTime;
            _maxConsecutiveShots = CurrentSelectedWeapon.maxConsecutiveShots;
            _recoveryDelay = CurrentSelectedWeapon.recoveryDelay;
            _recoveryRate = CurrentSelectedWeapon.recoveryRate;

            //Sets the current bullets to the saved bullets in the dictionary from this weapon
            if (_allWeaponBulletsStorage.TryGetValue(_weaponName, out var thisWeaponsBulletAmount))
            {
                //If there is already an Entry in the dictionary to this weaponname, currentAmmo = dictionaryWeapon ammo
                _currentBullets = thisWeaponsBulletAmount;
            }
            else
            {
                //Else currentAmmo = CurrentSelectedWeapon.maxBullets and create a dictionaryEntrance for this gun
                _currentBullets = (int)_maxBullets;
                _allWeaponBulletsStorage.Add(_weaponName, (int)_maxBullets); 
            }
            //Debug.Log($"Weapon Loaded: {_weaponName}. Bodyshot Damage: {_bodyshotDamage}");

            // TODO Fire event for UI that changes everything in PlayerHUD
        }
        else
        {
            Debug.LogError($"Current weapon is Null!{CurrentSelectedWeapon}");
        }
    }


    private void Shoot()
    {
        if (_currentBullets > 0)
        {
            //Debug.Log($"{_currentBullets} Bullets left, {_bodyshotDamage} damageamount");
            _currentBullets--;
        }
        else
        {
            if (!_isReloading)
            {
                _reloadingCoroutine = StartCoroutine(Reload());
            }
            return;
        }
        // TODO Fire Event that Shows current Bullets in PlayerUI HUD
        
        Ray shootingRay = new Ray(shootingStartingPos.position, GetInaccurateDirection(shootingStartingPos.forward));
        Debug.DrawRay(shootingStartingPos.position, GetInaccurateDirection(shootingStartingPos.forward) * 100, Color.red, 1f);

        if (Physics.Raycast(shootingRay, out RaycastHit hit, Mathf.Infinity))
        {
            // TODO Handle here that can only hit Enemies, Manager needs to give Players A Team! NO FRIENDLY FIRE!
            if (hit.collider.TryGetComponent(out PlayerInfo info) && info.playerType == PlayerType.TeamRed)
            {
                Vector3 enemyLocalPos = hit.collider.transform.position;
                PlayerShotServerRpc(hit.collider.GetComponent<NetworkObject>().NetworkObjectId, enemyLocalPos, _bodyshotDamage, NetworkManager.Singleton.LocalClientId);
            }
            else
            {
                //Hit Anything else than Enemy
                Debug.Log(hit.collider.gameObject);
            }
        }
        //Debug.Log($"Shooting with weapon: {_weaponName}. Bodyshot Damage: {_bodyshotDamage}");

    }
    private Vector3 GetInaccurateDirection(Vector3 originalDirection)
    {
        float inaccuracy = (_consecutiveShots / _maxConsecutiveShots) * _inaccuracyOnBulletSpam;
        Vector3 randomOffset = new Vector3(Random.Range(-inaccuracy, inaccuracy), Random.Range(-inaccuracy, inaccuracy), 0);
        return originalDirection + randomOffset;
    }

    private IEnumerator Reload()
    {
        // TODO Add Reload on R press
        Debug.Log($"Player is reloading for {_reloadTime} seconds");
        _isReloading = true;
        yield return new WaitForSeconds(_reloadTime);
        _currentBullets = (int)_maxBullets;
        _isReloading = false;
        _reloadingCoroutine = null;
    }

    [ServerRpc]
    private void PlayerShotServerRpc(ulong targetPlayerId, Vector3 enemyLocalPos, uint damageAmount, ulong shootersLocalClientID)
    {
        var targetPlayer = NetworkManager.SpawnManager.SpawnedObjects[targetPlayerId].GetComponent<PlayerHealth>();

        if (targetPlayer)
        {
            if (IsValidShot(targetPlayer.transform.position, enemyLocalPos))
            {
                ApplyDamageClientRpc(targetPlayerId, damageAmount, shootersLocalClientID);
            }
        }
    }

    private bool IsValidShot(Vector3 actualPosition, Vector3 perceivedPosition)
    {
        //Create a Tolerance foreach shot!
        const float tolerance = 0.5f;
        
        //True if hit withhin tolerance radius, false if missed
        return Vector3.Distance(actualPosition, perceivedPosition) <= tolerance;
    }


    [ClientRpc]
    private void ApplyDamageClientRpc(ulong targetplayerID, uint recievedDamage, ulong shootersLocalClientID)
    {
        var targetPlayer = NetworkManager.SpawnManager.SpawnedObjects[targetplayerID].GetComponent<PlayerHealth>();
        if (targetPlayer)
        {
            targetPlayer.TakeDamage(recievedDamage, shootersLocalClientID);
        }
    }


    
}
