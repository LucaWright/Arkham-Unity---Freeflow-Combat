using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState { Locomotion, Strike, Counter}
public class Player : MonoBehaviour
{
    UserInput   input;
    public PlayerState playerState;

    Camera cam;
    //Animator//
    Animator    animator;
    int         movingForwardHash;
    int         movingRightHash;

    Vector3     movementVector;

    private void Awake()
    {
        input = GetComponent<UserInput>();
        playerState = PlayerState.Locomotion;

        cam = Camera.main;

        animator = GetComponent<Animator>();
        movingForwardHash = Animator.StringToHash("Forward");
        movingRightHash = Animator.StringToHash("Right");
    }

    private void Update()
    {
        SetMovementVector();
        if (playerState == PlayerState.Locomotion)
        {
            CheckCombatInput();
        }
    }

    private void FixedUpdate()
    {
        switch (playerState)
        {
            case PlayerState.Locomotion:
                HandleMovement();
                HandleRotation();
                break;
            case PlayerState.Strike:
                CancelMovement();
                //Trova bersaglio
                //Se bersaglio è valido, attacca
                //Altrimenti, feedback "attacco errato"
                break;
            case PlayerState.Counter:
                CancelMovement();
                if (CombatDirector.state == CombatDirectorState.Executing)
                {
                    //Funzione contrattacco
                }
                else
                {
                    //Funzione feedback "contrattacco errato"
                }
                input.northButton = false; //mettere a false tutti i button, in qualche modo. Vedere se con input manager o altro
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

    void CheckCombatInput()
    {
        if (input.westButton)
            playerState = PlayerState.Strike;        
        if (input.northButton)
            playerState = PlayerState.Counter;        
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
    }
}
