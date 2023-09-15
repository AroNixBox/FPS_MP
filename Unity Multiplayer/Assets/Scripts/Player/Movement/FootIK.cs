using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Animator), typeof(CharacterController))]
public class FootIK : NetworkBehaviour
{
    private Animator _animator;
    private CharacterController _controller;
    private Transform _leftFoot, _rightFoot;

    [Header("Only Change if needed!")]  
    [SerializeField] private LayerMask groundLayer;
    [Range(0, 1)] [SerializeField] private float rightFootPositionWeight = 1;
    [Range(0, 1)] [SerializeField] private float rightFootRotationWeight = 1;
    [Range(0, 1)] [SerializeField] private float leftFootPositionWeight = 1;
    [Range(0, 1)] [SerializeField] private float leftFootRotationWeight = 1;
    [Range(0, 0.2f)] [SerializeField] private float footOffset = 0.1f;

    private float _ikBlend = 1f;

    [Tooltip("Blendspeed from animation to Ground-Snapping")]
    [SerializeField]
    private float toeAdjustmentSpeed = 5.0f;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _leftFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        _rightFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
    }

    //Will be called from Animator-Component
    private void OnAnimatorIK(int layerIndex)
    {
        if (!IsOwner) return;
        float modifiedWeight = 1f;

        if (IsCharacterMoving())
        {
            float animationProgress = _animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1;
            modifiedWeight = (Mathf.Sin(animationProgress * Mathf.PI * 2) + 1) / 2;
        }

        SetFootIK(AvatarIKGoal.RightFoot, rightFootPositionWeight * modifiedWeight, rightFootRotationWeight * modifiedWeight);
        SetFootIK(AvatarIKGoal.LeftFoot, leftFootPositionWeight * modifiedWeight, leftFootRotationWeight * modifiedWeight);
        AdjustLowerFootToGround();
    }

    private void AdjustLowerFootToGround()
    {
        Transform lowerFoot = (_leftFoot.position.y < _rightFoot.position.y) ? _leftFoot : _rightFoot;
        if (Physics.Raycast(lowerFoot.position + Vector3.up, Vector3.down, out RaycastHit hit, 2.5f, groundLayer))
        {
            float adjustmentAmount = hit.point.y - lowerFoot.position.y + footOffset;
            AdjustBonesRecursive(lowerFoot, adjustmentAmount);
        }
    }

    private void AdjustBonesRecursive(Transform bone, float adjustmentAmount)
    {
        bone.position += new Vector3(0, adjustmentAmount, 0);
        //This doesnt do much at this point. Idea was to adjust the Rigs position, but this only asks for the child
        /*foreach (Transform child in bone)
        {
            AdjustBonesRecursive(child, adjustmentAmount);
        }*/
    }


    private void SetFootIK(AvatarIKGoal foot, float positionWeight, float rotationWeight)
    {
        Ray ray = new Ray(_animator.GetIKPosition(foot) + Vector3.up * 0.5f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 2.5f, groundLayer))
        {
            Vector3 footPosition = hit.point + hit.normal * footOffset;
            Quaternion footRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, hit.normal), hit.normal);
            _animator.SetIKPosition(foot, footPosition);
            _animator.SetIKRotation(foot, footRotation);
            _animator.SetIKPositionWeight(foot, positionWeight * _ikBlend);
            _animator.SetIKRotationWeight(foot, rotationWeight * _ikBlend);

            Transform toeBone = foot == AvatarIKGoal.LeftFoot ? _leftFoot : _rightFoot;
            Vector3 targetToePosition = hit.point + hit.normal * footOffset;
            toeBone.position = Vector3.Lerp(toeBone.position, targetToePosition, toeAdjustmentSpeed * Time.deltaTime);

            float diffY = _animator.GetIKPosition(foot).y - footPosition.y;
            if (Mathf.Abs(diffY) > 0.1f)
            {
                transform.position -= new Vector3(0, diffY - 0.1f, 0);
            }
        }
        else
        {
            _animator.SetIKPositionWeight(foot, 0);
            _animator.SetIKRotationWeight(foot, 0);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        //if (!_controller.isGrounded) return;

        float target = IsCharacterMoving() ? 0f : 1f;
        _ikBlend = Mathf.Lerp(_ikBlend, target, Time.deltaTime * 2);
        UpdateIKBlendServerRpc(_ikBlend);
    }
    private void LateUpdate()
    {
        //if (!_controller.isGrounded) return;

        AdjustFootToGround(_leftFoot);
        AdjustFootToGround(_rightFoot);
    }
    
    private void AdjustFootToGround(Transform foot)
    {
        if (Physics.Raycast(foot.position + Vector3.up, Vector3.down, out RaycastHit hit, 2.5f, groundLayer))
        {
            float adjustmentAmount = hit.point.y - foot.position.y + footOffset;
            AdjustBonesRecursive(foot, adjustmentAmount);
        }
    }
    private bool IsCharacterMoving()
    {
        return _controller.velocity.magnitude > 0.1f; 
    }
    [ServerRpc]
    private void UpdateIKBlendServerRpc(float ikBlend, ServerRpcParams rpcParams = default)
    {
        _ikBlend = ikBlend;
        UpdateIKBlendClientRpc(_ikBlend);
    }
    [ClientRpc]
    private void UpdateIKBlendClientRpc(float ikBlend, ClientRpcParams rpcParams = default)
    {
        _ikBlend = ikBlend;
    }
}

