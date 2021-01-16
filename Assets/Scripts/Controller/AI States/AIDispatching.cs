using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class AIDispatching : State
{
    public AIDispatching(GameObject go, StateMachine fsm) : base(go, fsm) { }

    AgentAI agentAI;
    float stoppingDistance;

    private void Awake()
    {
        agentAI = GetComponent<AgentAI>();
    }

    void Start()
    {        
        go = this.gameObject;
        fsm = agentAI.fsm;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        agentAI.state = AgentState.Dispatching;
        stoppingDistance = agentAI.attackingState.attackRange; //vedere se e come utilizzarla
        agentAI.RunForward(stoppingDistance);
        //agentAI.agentNM.SetDestination(agentAI.target.position); //Deve accedere al navMesh. Meglio mettere un reference qui dentro

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

        agentAI.agentNM.SetDestination(agentAI.target.position); //Deve accedere al navMesh. Meglio mettere un reference qui dentro
        if (!agentAI.agentNM.hasPath) return;

        if (!agentAI.NavMeshDestinationReached()) //Probabilmente, meglio GLOBAL
        {
            agentAI.agentNM.SetDestination(agentAI.target.position);
            agentAI.canAttack = false;
            agentAI.animator.SetBool("Can Attack", agentAI.canAttack);
            return;
        }

        //IF REPOSITIONING: EXIT and RETURN
        //RICORDA: finché non finisce di rallentare, deve mantenere il navmesh path e aggiornare secondo le regole del navmesh, altrimenti slitta.

        agentAI.animator.ResetTrigger(agentAI.runningForwardHash);
        agentAI.animator.SetTrigger("Ready");
        agentAI.animator.SetBool("Can Attack", agentAI.canAttack);
        agentAI.movementDir = AgentMovementDir.None;
        agentAI.canAttack = true;
    }

    //funzione di emergenza di uscita!

    public override void OnExit()
    {
        base.OnExit();
        //agentAI.agentNM.speed = 0;
    }
}
