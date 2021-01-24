﻿using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.AI;

public class AIDispatching : State
{
    public AIDispatching(GameObject go, StateMachine fsm) : base(go, fsm) { }

    AgentAI agentAI;
    Animator animator;
    NavMeshAgent agentNM;
    SphereCollider avoidanceCollider;

    int idleHash;
    //int stopHash;

    int dispatchHash;
    int readyHash;
    int canAttackHash;

    float stoppingDistance;
    //AttackingState?
    //Prendere TARGET da DistanceInfo in CombatDirector

    private void Awake()
    {
        agentAI = GetComponent<AgentAI>();
    }

    void Start()
    {        
        go = this.gameObject;
        fsm = agentAI.fsm;

        go = this.gameObject;
        fsm = agentAI.fsm;
        animator = agentAI.animator;
        agentNM = agentAI.agentNM;
        avoidanceCollider = agentAI.avoidanceCollider;


        SetDispatchingHashParameters();
    }

    void SetDispatchingHashParameters()
    {
        idleHash = agentAI.idleHash;
        //stopHash = agentAI.stopHash;

        dispatchHash = Animator.StringToHash("Dispatch");
        readyHash = Animator.StringToHash("Ready");
        canAttackHash = Animator.StringToHash("Can Attack");
    }

    public override void OnEnter()
    {
        base.OnEnter();
        agentAI.state = AgentState.Dispatching;
        stoppingDistance = agentAI.attackingState.attackRange; //vedere se e come utilizzarla
        animator.ResetTrigger(readyHash); //Ogni tanto rimane qui. Lo faccio fare ad hit o, ancora meglio, qui. Così sono sicuro al 100%
        agentAI.RunForward(stoppingDistance);
    }

    public override void OnUpdate()
    {
        base.OnUpdate();        
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        agentAI.HandleNavMeshMovement();
        agentAI.HandleNavMeshRotation();

        agentNM.SetDestination(agentAI.target.position);
        if (!agentNM.hasPath) return;

        if (!agentAI.NavMeshDestinationReached())
        {
            agentNM.SetDestination(agentAI.target.position);
            agentAI.canAttack = false;
            animator.SetBool(canAttackHash, agentAI.canAttack);
            return;
        }

        animator.SetTrigger(readyHash);
        animator.SetBool(canAttackHash, agentAI.canAttack);
        agentAI.movementDir = AgentMovementDir.None;
        agentAI.canAttack = true;
    }

    public override void OnExit()
    {
        base.OnExit();
        agentAI.canAttack = false;
        animator.SetBool(canAttackHash, false);
        animator.ResetTrigger(readyHash);
    }
}
