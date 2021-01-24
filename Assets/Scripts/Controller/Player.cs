using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.InputSystem.LowLevel;


public enum PlayerState { Locomotion, Strike, Counter, Hit }
[SelectionBase]
public class Player : MonoBehaviour
{
    [HideInInspector] public UserInput   input;
    [HideInInspector] public PlayerState state;

    [HideInInspector] public StateMachine fsm;

    [HideInInspector] public PlayerLocomotion locomotionState;
    [HideInInspector] public PlayerCombat combatState;
    [HideInInspector] public PlayerCombatStrike combatStrike;
    [HideInInspector] public PlayerCombatCounter combatCounter;
    [HideInInspector] public PlayerOnHit hitState;

    [HideInInspector] public Camera cam;
    //Animator//
    [HideInInspector] public Animator animator;
    [HideInInspector] public int         movingForwardHash;
    [HideInInspector] public int         movingRightHash;

    [HideInInspector] public Transform           rootBone;
    [HideInInspector] public List<Rigidbody>     bodyParts;

    [HideInInspector] public float multicounterCount;

    [HideInInspector] public Vector3     movementVector = Vector3.zero;

    private void Awake()
    {
        input = GetComponent<UserInput>();
        state = PlayerState.Locomotion;
        cam = Camera.main;
        SetAnimator();
        SetRagdollRigidBodies();
        SetFiniteStateMachine();
    }

    void SetAnimator()
    {
        animator = GetComponent<Animator>();
        movingForwardHash = Animator.StringToHash("Forward");
        movingRightHash = Animator.StringToHash("Right");
    }
    void SetRagdollRigidBodies()
    {
        bodyParts = new List<Rigidbody>();

        rootBone = animator.GetBoneTransform(HumanBodyBones.Hips);
        Rigidbody[] bones = rootBone.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody bone in bones)
        {
            bodyParts.Add(bone);
            bone.isKinematic = true;
        }
    }

    void SetFiniteStateMachine()
    {
        fsm = new StateMachine();

        //idleState = GetComponent<PlayerIdle>();
        //Metti in uno scriptable? {
        locomotionState = GetComponent<PlayerLocomotion>();
        combatState = GetComponent<PlayerCombat>();
        combatStrike = GetComponent<PlayerCombatStrike>();
        combatCounter = GetComponent<PlayerCombatCounter>();
        hitState = GetComponent<PlayerOnHit>();
        //}

        fsm.State = locomotionState;        
    }

    private void Update()
    {
        SetMovementVector();
        fsm.State.OnUpdate();
    }

    private void FixedUpdate()
    {
        fsm.State.OnFixedUpdate();
    }

    public void SetMovementVector()
    {
        //La differenza con IA è che non deve essere rielaborato secondo la camera        
        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        movementVector = Vector3.ClampMagnitude(input.moveDir.x * right + input.moveDir.y * forward, 1f);
    }

    public bool CheckCombatInput()
    {
        if (input.westButton)
        {
            return true;
        }

        if (input.northButton)
        {
            return true;
        }

        return false;
    }

    public void ResetMovementParameters()
    {
        animator.SetFloat(movingForwardHash, 0);
        animator.SetFloat(movingRightHash, 0);
        input.westButton = false;
        input.northButton = false;
    }

    public void OnHit()
    {
        animator.ResetTrigger("Strike");
        animator.ResetTrigger("Strike Ender");
        animator.SetTrigger("Stun");
        fsm.State = hitState;
        CombatDirector.strikers.Clear();
    }



    //public void OnStunAnimationEvent()
    //{
    //    animator.ResetTrigger("Stun");
    //    CombatDirector.state = CombatDirectorState.Planning;
    //    state = playerState.Locomotion;
    //}

}
