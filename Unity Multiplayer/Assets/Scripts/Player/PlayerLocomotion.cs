using System;
using System.Collections.Generic;
using Cinemachine;
using Unity.Collections;
using Unity.Netcode;
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
    private float _accelerationFactor = 2f;
    private float _currentSpeed;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float additionalGravity = 8f;
    private float _fallSpeed;
    private const float GravityForce = 20f;
    private const float IncreasedGravityAfter = 1.5f;
    private bool _wasGroundedLastFrame;
    private float _notGroundedTimer;


    [Header("References")]
    private Animator _animator;
    private CharacterController _characterController;
    [SerializeField] private GameObject _camera;
    [SerializeField] private CinemachineVirtualCamera vcam;
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
        
        vcam.enabled = true;
    }

    void Update()
    {
        if (!IsOwner) return;
        Move();
        Jump();
        HandleGravity();
    }


    private void Move()
    {
        Vector3 moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        moveDirection = _camera.transform.TransformDirection(moveDirection);
        moveDirection.y = 0;

        //If were moving, Lerp our speed up to maxspeed with accFactor per sec, else Lerp our speed down to 0 by that same accFactor.
        _currentSpeed = moveDirection.magnitude > 0.1f
            ? Mathf.Lerp(_currentSpeed, maxSpeed, Time.deltaTime * _accelerationFactor)
            : Mathf.Lerp(_currentSpeed, 0, Time.deltaTime * _accelerationFactor);

        _characterController.Move(moveDirection.normalized * (_currentSpeed * Time.deltaTime));

        _animator.SetFloat(Speed, _currentSpeed);
    }

    private void Jump()
    {
        if (IsPlayerGrounded() && Input.GetButtonDown("Jump"))
        {
            _animator.SetBool(AnimJump, true);
            //MoveVertically Method automatically handles the Jump if _fallSpeed is increased here
            _fallSpeed = jumpForce;
            //_animator.SetBool(AnimJump,  true);
            if (_footIK.enabled)
                _footIK.enabled = false;
        }
        
    }
    private bool IsPlayerGrounded()
    {
        Vector3 spherePosition = transform.position + groundCheckOffset;
        Collider[] hitColliders = Physics.OverlapSphere(spherePosition, groundCheckDistance, groundLayer);
        return hitColliders.Length > 0;
    }
    //Bad written, needs refactor
    private void HandleGravity()
    {
        if (!IsPlayerGrounded() || _fallSpeed > 0.0f)
        {
            _fallSpeed -= GravityForce * Time.deltaTime;

            if (transform.position.y > IncreasedGravityAfter && _fallSpeed < 0)
                _fallSpeed -= additionalGravity * Time.deltaTime;

            _notGroundedTimer -= Time.deltaTime;
            float notGroundedTimerMax = 0.2f;
            if (_notGroundedTimer < 0f)
            {
                _notGroundedTimer = notGroundedTimerMax;
                if (_footIK.enabled)
                    _footIK.enabled = false;
            }
        }
        else
        {
            _fallSpeed = -0.1f;
            if (!_footIK.enabled)
                _footIK.enabled = true;
            _animator.SetBool(AnimJump, false);
        }

        _wasGroundedLastFrame = IsPlayerGrounded();
        bool isMovingVertically =
            _fallSpeed > 0.0f || !IsPlayerGrounded() || (!_wasGroundedLastFrame && IsPlayerGrounded());

        if (isMovingVertically)
        {
            _characterController.Move(Vector3.up * (_fallSpeed * Time.deltaTime));
        }
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
