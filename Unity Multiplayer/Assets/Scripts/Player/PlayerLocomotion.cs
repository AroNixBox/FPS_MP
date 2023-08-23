using System;
using System.Collections.Generic;
using Cinemachine;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;


public class PlayerLocomotion : NetworkBehaviour
{
    [SerializeField] private float speed;
    private float _accelerationFactor = 2f;
    private float _currentSpeed;
    private const float gravity = 9.81f;

    private Animator _animator;
    private CharacterController _characterController;
    [SerializeField] private GameObject _camera;

    [SerializeField]
    private CinemachineVirtualCamera vcam;

    //Pooling Later on
    [SerializeField] private Transform spawnedObjectPrefab;
    private Transform _spawnedObjectTransform;
    
    private NetworkVariable<MyCustomData> _randomNumber = new NetworkVariable<MyCustomData>(new MyCustomData{ INT = 56, Bool = true}, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (!IsOwner) return; 
        
        vcam.enabled = true;
    }

    void Update()
    {
        //Maybe put this on top
        if (!IsOwner) return;
        if (!_characterController.isGrounded)
        {
            _characterController.Move(Vector3.down * gravity * Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            RequestSpawnObjectServerRpc();
        }

        
        //Movement
        Vector3 moveDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        moveDir = _camera.transform.TransformDirection(moveDir);
        moveDir.y = 0;

        if (moveDir.magnitude > 0.1f)
            _currentSpeed = Mathf.Lerp(_currentSpeed, speed, Time.deltaTime * _accelerationFactor);
        else 
            _currentSpeed = Mathf.Lerp(_currentSpeed, 0, Time.deltaTime * _accelerationFactor);

        _characterController.Move(moveDir.normalized * (_currentSpeed * Time.deltaTime));

        _animator.SetFloat("Speed", _currentSpeed);


    }

    //Code Only runs on the Server/ Host, not on the Client itself
    //Needs to be RPC in the End!
    [ServerRpc]
    private void RequestSpawnObjectServerRpc(ServerRpcParams serverRpcParams = default) 
    {
        SpawnObjectClientRpc();
    }
    [ClientRpc]
    private void SpawnObjectClientRpc(ClientRpcParams clientRpcParams = default)
    {
        _spawnedObjectTransform = Instantiate(spawnedObjectPrefab);
        _spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
    }

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
