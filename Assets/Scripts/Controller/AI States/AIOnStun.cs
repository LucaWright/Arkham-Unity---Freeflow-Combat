using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class AIOnStun : State
{
    public AIOnStun(GameObject go, StateMachine fsm) : base(go, fsm) { }

    AgentAI agentAI;

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
        agentAI.state = AgentState.Stun;
        agentAI.animator.SetTrigger("Stunned");
        agentAI.ResetNavMeshPath();
    }

    public override void OnUpdate()
    {
        base.OnUpdate();        
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
    }
}
