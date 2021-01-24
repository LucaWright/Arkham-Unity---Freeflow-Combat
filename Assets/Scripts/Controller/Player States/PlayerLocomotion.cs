using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLocomotion : State
{
    public PlayerLocomotion (GameObject go, StateMachine fsm) : base(go, fsm) { }

    public float accelerationDamp = .15f;
    public float decelerationDamp = .1f;

    Player player;

    private void Awake()
    {
        player = GetComponent<Player>();        
    }

    void Start()
    {
        go = this.gameObject;
        fsm = player.fsm;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        player.state = PlayerState.Locomotion;
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        //HANDLE INPUT
        if (player.CheckCombatInput())
        {
            // => CombatState (Process Input) => CombatSubState (Strike or Counter)
            player.fsm.State = player.combatState;
        }
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();        

        if (player.movementVector != Vector3.zero)
        {
            HandleMovement(accelerationDamp);
            HandleRotation();
        }
        else
        {
            //decelera fino a che i float non diventano 0. Quando i float diventano 0...
            HandleMovement(decelerationDamp);
        }

        
    }

    void HandleMovement(float dampValue)
    {
        player.animator.SetFloat(player.movingForwardHash, transform.InverseTransformDirection(player.movementVector).z, dampValue, Time.fixedDeltaTime); //controllare come funziona il damp
        player.animator.SetFloat(player.movingRightHash, transform.InverseTransformDirection(player.movementVector).x, dampValue, Time.fixedDeltaTime); //rotation damp?
    }

    void HandleRotation() //differenziarla tra rotazione locomotion e fight
    {
        Quaternion lookRotation = Quaternion.LookRotation(player.movementVector, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, .1f);
    }
}
