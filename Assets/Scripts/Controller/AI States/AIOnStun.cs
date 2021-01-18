using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class AIOnStun : State
{
    public AIOnStun(GameObject go, StateMachine fsm) : base(go, fsm) { }

    AgentAI agentAI;
    Animator animator;

    int stunnedHash;

    private void Awake()
    {
        agentAI = GetComponent<AgentAI>();
    }

    void Start()
    {        
        go = this.gameObject;
        fsm = agentAI.fsm;
        animator = agentAI.animator;

        SetOnStunHashParameters();

    }

    void SetOnStunHashParameters()
    {
        stunnedHash = Animator.StringToHash("Stunned");
    }

    public override void OnEnter()
    {
        base.OnEnter();
        agentAI.state = AgentState.Stun;
        animator.SetTrigger(stunnedHash);
        agentAI.ResetNavMeshPath();
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        agentAI.HandleRootMotionMovement();
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
    }
}
