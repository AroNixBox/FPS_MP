using System;
using System.Collections.Generic;
using Cinemachine;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;


public class PlayerLocomotion : NetworkBehaviour
{
    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, -1, 0);
    [SerializeField] private LayerMask groundLayer;
    
    [FormerlySerializedAs("speed")]
    [Header("Locomotion")]
    [SerializeField] private float maxSpeed;
    private readonly float _accelerationFactor = 2f;
    private float _currentSpeed;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float additionalGravity = 8f;
    private float _fallSpeed;
    private const float GravityForce = 20f;
    private const float IncreasedGravityAfter = 1.5f;
    private bool _wasGroundedLastFrame;
    private float _notGroundedTimer;
    private const float LandingThreshold = 2f;


    [Header("References")]
    private Animator _animator;
    private CharacterController _characterController;
    //Both the same Vcameras
    [SerializeField] private GameObject _camera;
    [SerializeField] private CinemachineVirtualCamera playerVirtualCamera;
    private FootIK _footIK;

    //Not needed
    [SerializeField] private Transform spawnedObjectPrefab;
    private Transform _spawnedObjectTransform;
    
    private NetworkVariable<MyCustomData> _randomNumber = new NetworkVariable<MyCustomData>(new MyCustomData{ INT = 56, Bool = true}, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int AnimJump = Animator.StringToHash("Jump");


    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _footIK = GetComponent<FootIK>();
    }

    private void Start()
    {
        if (!IsOwner) return; 
        
        playerVirtualCamera.enabled = true;
    }

    void Update()
    {
        if (!IsOwner) return;

        HandlePlayerMovement();
        HandleJumpInput();
        if (!_wasGroundedLastFrame && IsPlayerGrounded() && Mathf.Abs(_fallSpeed) > LandingThreshold)
        {
            TriggerLandingAnimation();
        }
        _wasGroundedLastFrame = IsPlayerGrounded();
    }
    private void TriggerLandingAnimation()
    {
        Debug.Log("Landed");
    }

    private void HandlePlayerMovement()
    {
        bool isGrounded = IsPlayerGrounded();
        // Horizontal Movement
        Vector3 moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        moveDirection = _camera.transform.TransformDirection(moveDirection);
        moveDirection.y = 0;
        _currentSpeed = moveDirection.magnitude > 0.1f 
            ? Mathf.Lerp(_currentSpeed, maxSpeed, Time.deltaTime * _accelerationFactor)
            : Mathf.Lerp(_currentSpeed, 0, Time.deltaTime * _accelerationFactor);
    
        // Vertical Movement & Gravity
        if (!isGrounded || _fallSpeed > 0.0f)
        {
            _fallSpeed -= GravityForce * Time.deltaTime;
            if (transform.position.y > IncreasedGravityAfter && _fallSpeed < 0)
                _fallSpeed -= additionalGravity * Time.deltaTime;
        }
        else
        {
            
            _fallSpeed = -0.1f;
            _animator.SetBool(AnimJump, false);
        }

        Vector3 finalMove = moveDirection.normalized * (_currentSpeed * Time.deltaTime) + Vector3.up * (_fallSpeed * Time.deltaTime);
        _characterController.Move(finalMove);
    
        // Animation
        _animator.SetFloat(Speed, _currentSpeed);

        if (isGrounded)
            ToggleIKServerRpc(true);
        else
            ToggleIKServerRpc(false);
    }
    private void HandleJumpInput()
    {
      if (IsPlayerGrounded() && Input.GetButtonDown("Jump"))
      {
          _animator.SetBool(AnimJump, true);
          _fallSpeed = jumpForce;
      }
    }

    private bool IsPlayerGrounded()
    {
        Vector3 spherePosition = transform.position + groundCheckOffset;
        Collider[] hitColliders = Physics.OverlapSphere(spherePosition, groundCheckDistance, groundLayer);
        return hitColliders.Length > 0;
    }
    
    [ServerRpc]
    public void ToggleIKServerRpc(bool isActive, ServerRpcParams rpcParams = default)
    {
        ToggleIK(isActive);
        ToggleIKClientRpc(isActive);
    }

    [ClientRpc]
    public void ToggleIKClientRpc(bool isActive, ClientRpcParams rpcParams = default)
    {
        ToggleIK(isActive);
    }

    private void ToggleIK(bool isActive)
    {
        _footIK.enabled = isActive;
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + groundCheckOffset, groundCheckDistance);
    }

    //Code Only runs on the Server/ Host, not on the Client itself
    //Needs to be RPC in the End!
    /*[ServerRpc]
    private void RequestSpawnObjectServerRpc(ServerRpcParams serverRpcParams = default) 
    {
        SpawnObjectClientRpc();
    }
    [ClientRpc]
    private void SpawnObjectClientRpc(ClientRpcParams clientRpcParams = default)
    {
        _spawnedObjectTransform = Instantiate(spawnedObjectPrefab);
        _spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
    }*/

    public override void OnNetworkSpawn()
    {
        _randomNumber.OnValueChanged += (MyCustomData previousValue, MyCustomData newValue) =>
        {
            Debug.Log(OwnerClientId + "; " +  newValue.INT+ "; " + newValue.Bool+ "; " + newValue.Message+ ";");
        };
    }
    
    public struct MyCustomData : INetworkSerializable
    {
        public int INT;
        public bool Bool;
        public FixedString128Bytes Message;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref INT);
            serializer.SerializeValue(ref Bool);
            serializer.SerializeValue(ref Message);
        }
    }
}
