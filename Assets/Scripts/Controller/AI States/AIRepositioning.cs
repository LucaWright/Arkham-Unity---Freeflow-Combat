using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIRepositioning : State
{
    public AIRepositioning(GameObject go, StateMachine fsm) : base(go, fsm) { }

    AgentAI agentAI;
    Animator animator;
    NavMeshAgent agentNM;
    SphereCollider avoidanceCollider;

    int idleHash;
    int stopHash;

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
        stopHash = agentAI.stopHash;

        dispatchHash = Animator.StringToHash("Dispatch");
        stopHash = Animator.StringToHash("Stop");
    }

    public override void OnEnter()
    {
        base.OnEnter();
        agentAI.state = AgentState.Repositioning;
        //stoppingDistance = CombatDirector.DistanceInfo.LastLineRadius;
        //agentAI.RunForward(stoppingDistance);
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
        }

        if (agentAI.currentLine > CombatDirector.DistanceInfo.Lines) return;

        animator.SetTrigger(stopHash);

        fsm.State = agentAI.locomotionState;
    }

    public override void OnExit()
    {
        base.OnExit();
        agentAI.ResetNavMeshPath();
    }
}
