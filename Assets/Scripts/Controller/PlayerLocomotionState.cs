using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerLocomotionState : State
{
    public PlayerLocomotionState(GameObject go, StateMachine sm) : base(go, sm) { }

    UserInput input;

    Animator animator;
    int movingForwardHash;
    int movingRightHash;

    Vector3 movementVector;

    private void Awake()
    {
        input = GetComponent<UserInput>();

        animator = GetComponent<Animator>();
        movingForwardHash = Animator.StringToHash("Forward");
        movingRightHash = Animator.StringToHash("Right");
    }

    public override void OnEnter()
    {
        base.OnEnter();
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        SetMovementDirection();
        
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        HandleMovement();
        HandleRotation();
    }

    public override void OnExit()
    {
        base.OnExit();
    }


    void SetMovementDirection()
    {
        Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward); //Non mi convince. Dovrebbe essere il contrario. Indagare cosa ho capito sbagliato.

        movementVector = input.moveDir.x * right + input.moveDir.y * forward;
    }

    void HandleMovement()
    {
        animator.SetFloat(movingForwardHash, transform.InverseTransformDirection(movementVector).z, .15f, Time.fixedDeltaTime);
    }

    void HandleRotation()
    {
        animator.SetFloat(movingRightHash, transform.InverseTransformDirection(movementVector).x, .15f, Time.fixedDeltaTime);
        if (movementVector != Vector3.zero) //C'è un modo migliore senza if?
        {
            Quaternion lookRotation = Quaternion.LookRotation(movementVector, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, .1f);
        }
    }
}
