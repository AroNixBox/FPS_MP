using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class OwnerNetworkAnimator : NetworkAnimator
{
    private void Start()
    {
        NetworkManager.Singleton.LogLevel = LogLevel.Normal;
    }

    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
    
}