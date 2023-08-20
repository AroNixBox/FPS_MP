using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerLook : NetworkBehaviour
{
    [SerializeField] private float mouseSensitivity = 100.0f;
    
    [Header("Mesh-Rotation")] 
    [SerializeField] private Transform headBone;
    [Tooltip("PlayerParent")] 
    [SerializeField] private Transform characterTransform;
    
    [Header("Mesh-Rotation")]
    [Tooltip("Degree angle, when reached, body starts rotating to fov")]
    [SerializeField] private float bodyFollowThreshold = 45f;
    [Tooltip("Speed of rotation when body rotation is adjusted to fov rotation")]
    [SerializeField] private float rotationSpeed = 5.0f;
    
    private float _xRotation;             
    private float _yRotation;             
    private bool _isRotatingCharacter;
    private void Update()
    {
        if (!IsOwner) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        if (Mathf.Approximately(mouseX, 0f) && Mathf.Approximately(mouseY, 0f))
            return;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        _yRotation += mouseX;

        Quaternion playerRotation = Quaternion.Euler(_xRotation, _yRotation, 0f);
        transform.rotation = playerRotation;

        float deltaRotation = Mathf.DeltaAngle(characterTransform.eulerAngles.y, _yRotation);
        
        if (Mathf.Abs(deltaRotation) > bodyFollowThreshold)
        {
            _isRotatingCharacter = true;
        }

        if (_isRotatingCharacter)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, _yRotation, 0f);
            characterTransform.rotation = Quaternion.Slerp(characterTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            if (Quaternion.Angle(characterTransform.rotation, targetRotation) < 0.5f)
            {
                characterTransform.rotation = targetRotation;
                _isRotatingCharacter = false;
            }
        }

        headBone.rotation = playerRotation;
        UpdateHeadRotationServerRpc(headBone.rotation);
    }
    [ServerRpc]
    private void UpdateHeadRotationServerRpc(Quaternion newRotation, ServerRpcParams rpcParams = default)
    {
        // Senden Sie die neue Rotation an alle Clients
        UpdateHeadRotationClientRpc(newRotation);
    }

    [ClientRpc]
    private void UpdateHeadRotationClientRpc(Quaternion newRotation, ClientRpcParams rpcParams = default)
    {
        // Aktualisieren Sie die Rotation des Kopfes auf allen Clients
        headBone.rotation = newRotation;
    }
}
