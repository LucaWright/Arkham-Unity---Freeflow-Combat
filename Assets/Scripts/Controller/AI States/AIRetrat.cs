using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class AIRetrat : State
{
    public AIRetrat(GameObject go, StateMachine fsm) : base(go, fsm) { }

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
        agentAI.state = AgentState.Retreat;
        StartCoroutine(RetreatCheckOut());
    }

    public override void OnUpdate()
    {
        base.OnUpdate(); //Rivedere il distance handler, dicendo che se SUPERA 1, allora è zero (floor anziché rount)
        agentAI.HandleRootMotionMovement(); //root motion
        agentAI.HandleRootMotionRotation(); //root motion
    }

    IEnumerator RetreatCheckOut()
    {
        while (agentAI.currentLine < agentAI.destinationLine)
        {
            yield return new WaitForFixedUpdate();
        }

        agentAI.BackToIdle();
        
        while (!agentAI.animator.IsInTransition(0))
        {
            yield return new WaitForFixedUpdate();
        }
        yield return new WaitForSeconds(agentAI.animator.GetAnimatorTransitionInfo(0).duration);
        yield return new WaitForFixedUpdate();
        fsm.State = agentAI.idleState;
        //Debug.Break();
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
    }

    public override void OnExit()
    {
        base.OnExit();
        StopAllCoroutines();
    }
}
