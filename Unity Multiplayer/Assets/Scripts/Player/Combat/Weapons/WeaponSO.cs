using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/Weapon")]
public class WeaponSO : ScriptableObject
{
    [Tooltip("Displayname of the Gun")]
    public string weaponName;
    [Tooltip("Firerate: how fast the shots can be fired")]
    public float minTimeBetweenShots;
    [Tooltip("Damage on Bodyshot")]
    public uint bodyshotDamage;
    [Tooltip("Damage on Headshot")]
    public uint headshotDamage;
    [Tooltip("Max weaponinaccuracy Spam-Firing")]
    public float inaccuracyOnBulletSpam;
    [Tooltip("Max Bullets per magazine")]
    public uint maxBullets;
    [Tooltip("The time it takes for reloading")]
    public float reloadTime;
    [Tooltip("Max Shots/ Recoil Shot-Grenze. Has reached max Recoil after this amount of shots fired. " +
             "The amount of shots fired one after another before reaching the inaccuracyOnBulletSpam. " +
             "The higher this is the More bullets you can fire before getting inaccurate. " +
             "Put Small number for SingleShot guns like revolver, put higher number for Mps")]
    public uint maxConsecutiveShots;
    [Tooltip("Time before gun starts recovery proccess from Recoil")]
    public float recoveryDelay;
    [Tooltip("How many Shots should be recovered per second? The higher the faster the gun will cool down from weaponspam")]
    public float recoveryRate;
    //Add Aiming stuff...
}
