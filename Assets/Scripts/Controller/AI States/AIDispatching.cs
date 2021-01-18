using System.Collections;
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
    int stopHash;

    int runningForwardHash;
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

        runningForwardHash = Animator.StringToHash("Run Forward");
        readyHash = Animator.StringToHash("Ready");
        canAttackHash = Animator.StringToHash("Can Attack");
    }

    public override void OnEnter()
    {
        base.OnEnter();
        agentAI.state = AgentState.Dispatching;
        stoppingDistance = agentAI.attackingState.attackRange; //vedere se e come utilizzarla
        agentAI.StopAllCoroutines(); //Dovrebbe risolvere il problema del retreating. Anche se... DOVREBBE FARLO GIA' RUNNING FORWARD!
        animator.ResetTrigger(readyHash); //Ogni tanto rimane qui. Lo faccio fare ad hit o, ancora meglio, qui. Così sono sicuro al 100%
        agentAI.RunForward(stoppingDistance);

        //TODO
        //Mettere enum per i due tipi di dispatching?
        //Uno è dispatching di attacco, uno di riposizionamento.
        //Entrambi sfruttano il navmesh e fanno operazioni simili, tranne che per i check finali.
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

        //IF REPOSITIONING: EXIT and RETURN
        //RICORDA: finché non finisce di rallentare, deve mantenere il navmesh path e aggiornare secondo le regole del navmesh, altrimenti slitta.

        animator.ResetTrigger(agentAI.runningForwardHash);
        animator.SetTrigger(readyHash);
        animator.SetBool(canAttackHash, agentAI.canAttack);
        agentAI.movementDir = AgentMovementDir.None;
        agentAI.canAttack = true;
    }

    public override void OnExit()
    {
        base.OnExit();
        agentAI.canAttack = false;
        animator.SetBool(canAttackHash, agentAI.canAttack);
        animator.ResetTrigger(readyHash);
    }
}
