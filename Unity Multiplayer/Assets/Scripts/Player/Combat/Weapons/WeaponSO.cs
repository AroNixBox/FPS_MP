using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/Weapon")]
public class WeaponSO : ScriptableObject
{
    public string weaponName;
    public float minTimeBetweenShots;
    public uint bodyshotDamage;
    public uint headshotDamage;
    public float inaccuracyOnBulletSpam;
    public uint maxBullets;
    public float reloadTime;
    //Add Aiming stuff...
}
