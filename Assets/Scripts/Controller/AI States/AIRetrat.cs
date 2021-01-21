using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class AIRetrat : State
{
    public AIRetrat(GameObject go, StateMachine fsm) : base(go, fsm) { }

    AgentAI agentAI;
    Animator animator;

    int idleHash;

    private void Awake()
    {
        agentAI = GetComponent<AgentAI>();
    }

    void Start()
    {        
        go = this.gameObject;
        fsm = agentAI.fsm;
        animator = agentAI.animator;

        idleHash = agentAI.idleHash;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        agentAI.state = AgentState.Retreat;
        //Avverte chi è dietro di sè (se in Idle o Positioning) di andare in retreat a seconda di alcuni casi
        StartCoroutine(RetreatCheckIn());
        //StartCoroutine(RetreatCheckOut());
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        agentAI.HandleRootMotionMovement(); //root motion
        agentAI.HandleRootMotionRotation(); //root motion

        //TODO vedere se mettere anche qui il check hasGreenLightToTarget
    }

    IEnumerator RetreatCheckIn()
    {
        while (!animator.IsInTransition(0))
        {
            yield return new WaitForFixedUpdate();
        }
        yield return new WaitForSeconds(animator.GetAnimatorTransitionInfo(0).duration);
        StartCoroutine(RetreatCheckOut());
    }

    IEnumerator RetreatCheckOut()
    {
        while (agentAI.currentLine < agentAI.destinationLine)
        {
            yield return new WaitForFixedUpdate();
        }
        //StartCoroutine(agentAI.BackToIdle());
        agentAI.BackToIdle();
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
    }

    //FUNZIONE COMUNICAZIONE CON AI DIETRO DI SE'

    public override void OnExit()
    {
        base.OnExit();
        //animator.ResetTrigger(idleHash);
        StopAllCoroutines();
    }
}
