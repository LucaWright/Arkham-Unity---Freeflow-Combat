using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.InputSystem.LowLevel;

public enum PlayerState { Locomotion, Strike, Counter}

[SelectionBase]
public class Player : MonoBehaviour
{
    UserInput   input;
    public PlayerState state;

    Camera cam;
    //Animator//
    Animator    animator;
    int         movingForwardHash;
    int         movingRightHash;

    Transform           rootBone;
    List<Rigidbody>     bodyParts;

    float multicounterCount;

    Vector3     movementVector;
    AgentAI target;
    public float stoppingDistance = 1.5f;
    public float attackDuration = .75f;
    public Ease jumpEase;
    public LayerMask raycastMask;

    //Mettere Unity Event?
    public Action PlayerFX;
    public ParticleSystem batFX;

    private void Awake()
    {
        input = GetComponent<UserInput>();
        state = PlayerState.Locomotion;
        cam = Camera.main;
        SetAnimator();
        SetRagdollRigidBodies();
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

    private void Update()
    {
        SetMovementVector();
        //if (state == PlayerState.Locomotion)
        //{
        //    CheckCombatInput();
        //}
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case PlayerState.Locomotion: //Fare l'animation event sul blend tree?
                CheckCombatInput();
                HandleMovement();
                HandleRotation(); //Tutto 'sto casino... per te?
                break;
            case PlayerState.Strike:
                
                break;
            case PlayerState.Counter:
                
                break;
            default:
                break;
        }                
    }

    void SetMovementVector()
    {
        //Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        //Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        movementVector = Vector3.ClampMagnitude(input.moveDir.x * right + input.moveDir.y * forward, 1f);
    }

    public void CheckCombatInput()
    {
        if (input.westButton)
            StrikeCheck();
        if (input.northButton)
            CounterCheck();
    }

    void StrikeCheck()
    {
        if (FindTarget())
        {
            CancelMovement();

            state = PlayerState.Strike;
            animator.SetTrigger("Strike");

            StopAllCoroutines();
            StartCoroutine(Strike());
        }
        else
        {
            input.westButton = false;
            //false input;
        }
    }

    void CounterCheck()
    {
        if (CombatDirector.state == CombatDirectorState.Executing)
        {
            state = PlayerState.Counter;
            CancelMovement();
            CounterOnce();
            StartCoroutine(Counter());            
        }
        else
        {
            input.northButton = false;
            //false input
            return;
        }        
    }

    void CounterOnce()
    {
        var thug = CombatDirector.strikers.ElementAt(0);
        thug.SetUICounter(false);
        CombatDirector.strikers.Remove(thug);
        thug.Stun();
        input.northButton = false;
    }

    void HandleMovement()
    {
        animator.SetFloat(movingForwardHash, transform.InverseTransformDirection(movementVector).z, .15f, Time.fixedDeltaTime); //controllare come funziona il damp
    }

    void HandleRotation() //differenziarla tra rotazione locomotion e fight
    {
        animator.SetFloat(movingRightHash, transform.InverseTransformDirection(movementVector).x, .15f, Time.fixedDeltaTime);
        if (movementVector != Vector3.zero) //C'è un modo migliore senza if?
        {
            Quaternion lookRotation = Quaternion.LookRotation(movementVector, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, .1f);
        }
    }

    void CancelMovement()
    {
        animator.SetFloat(movingForwardHash, 0);
        animator.SetFloat(movingRightHash, 0);
        input.westButton = false;
        input.northButton = false;
    }

    public void Stun()
    {
        animator.SetTrigger("Stun");
        CombatDirector.strikers.Clear();
        //Set animation trigger
        //Event su animation: riporta il combat director in planning
    }

    public void OnStunAnimationEvent()
    {
        animator.ResetTrigger("Stun");
        CombatDirector.state = CombatDirectorState.Planning;
        state = PlayerState.Locomotion;
    }

    Vector3 GetDistanceToTarget()
    {
        return (target.transform.position - transform.position);
    }

    IEnumerator Strike()
    {
        float easedValue = 0;
        float attackPercentage = 0;

        Vector3 startingAttackPosition = transform.position;
        Vector3 vecToTarget = GetDistanceToTarget();
        Vector3 separator = -vecToTarget.normalized * stoppingDistance;

        //Handle rotation
        transform.rotation = Quaternion.LookRotation(vecToTarget, Vector3.up);

        while (attackPercentage / attackDuration < 1)
        {
            attackPercentage += Time.fixedDeltaTime;
            easedValue = DOVirtual.EasedValue(0, 1, Mathf.Clamp01(attackPercentage / attackDuration), jumpEase);
            transform.position = Vector3.Lerp(startingAttackPosition, target.transform.position + separator, easedValue);
            yield return new WaitForFixedUpdate();
        }

        animator.SetTrigger("Strike Ender");
        
        //Event su Target!
        //VFX
        //SFX
        //Polish!
        PlayerFX?.Invoke();
        input.westButton = false; //farglielo fare all'input manager? 
        input.northButton = false;

        target.Hit(); //e se era l'unico striker???

        yield return ImpactTimeFreeze();

        float timer = 0;
        while (timer < .5f)
        {
            timer += Time.fixedDeltaTime;
            CheckCombatInput();
            yield return new WaitForFixedUpdate();
        }

        state = PlayerState.Locomotion;
    }

    IEnumerator Counter() //check input
    {
        //COUNTER SETUP
        animator.SetTrigger("Counter Setup");
        batFX.Play();
        //Debug.Break();

        float remainingBeat = attackDuration; // prenderà da variabile del beat master
        //In realtà, dovrebbe partire dall'inizio dell'attacco.

        //Continue only when all strikers are countered
        while (CombatDirector.strikers.Count != 0)
        {
            remainingBeat -= Time.fixedDeltaTime;
            if (input.northButton)
            {
                CounterOnce();
            }
            yield return new WaitForFixedUpdate();
        }
        //End setup
        yield return new WaitForSeconds(remainingBeat);

        //COUNTERING
        animator.SetTrigger("Counter");
        //Wait animation transition
        yield return new WaitForFixedUpdate();

        //Debug.Log(animator.GetAnimatorTransitionInfo(0).duration);
        //Debug.Break();

        //Get animation transition duration and wait
        yield return new WaitForSeconds(animator.GetAnimatorTransitionInfo(0).duration); // + fixedDeltaTime
        //Wait for complete transition ending
        yield return new WaitForFixedUpdate();
        //Debug.Log(animator.GetCurrentAnimatorClipInfo(0)[0].clip.name);
        //Debug.Log(animator.GetCurrentAnimatorClipInfo(0)[0].clip.length);
        //Debug.Log(animator.GetNextAnimatorStateInfo(0).length);

        //float timer = 0;
        
        //Debug.Break();
        RaycastHit hitInfo;
        //Hit raycast for the duration of the counter animation
        while (!animator.IsInTransition(0)) //IDEALE: fino a inizio transition di uscita! Praticamente, fino all'enter del nuovo stato!
        {
            //timer += Time.fixedDeltaTime;
            Debug.DrawRay(rootBone.position, rootBone.forward * stoppingDistance, Color.red, .02f);
            if (Physics.Raycast(rootBone.position, rootBone.forward, out hitInfo, 1.5f, raycastMask))
            {
                hitInfo.transform.root.GetComponent<AgentAI>().Hit();

                yield return ImpactTimeFreeze();
            }
            yield return new WaitForFixedUpdate();
        }
        //Then... transition

        //COUNTER ENDER
        //Debug.Break();
        yield return new WaitForSeconds(animator.GetAnimatorTransitionInfo(0).duration); //+ fixedDeltaTime
        yield return new WaitForFixedUpdate();
        while (!animator.IsInTransition(0))
        {
            yield return new WaitForFixedUpdate();
        }
        //Debug.Break();
        yield return new WaitForSeconds(animator.GetAnimatorTransitionInfo(0).duration);
        //Debug.Break();
        state = PlayerState.Locomotion;
    }

    IEnumerator ImpactTimeFreeze()
    {
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(.04f);
        Time.timeScale = 1;
    }

    void CounterEndedAnimationEvent() //EVENT
    {
        CombatDirector.state = CombatDirectorState.Planning;
        //state = PlayerState.Locomotion;
    }

    public void SetState(PlayerState playerState)
    {
        state = playerState;
    }

    bool FindTarget()
    {
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position + Vector3.up, .75f, movementVector, out hitInfo, 20f, raycastMask))
        {
            target = hitInfo.transform.GetComponent<AgentAI>();
            return true;
        }
        return false;
    }


}
