using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class AIOnHit : State
{
    public AIOnHit(GameObject go, StateMachine fsm) : base(go, fsm) { }

    AgentAI agentAI;

    public float hitExitTime = 3f;

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
        agentAI.state = AgentState.Hit;
        agentAI.avoidanceCollider.enabled = false;
        agentAI.agentNM.enabled = false;
        agentAI.ResetNavMeshPath();
        agentAI.animator.enabled = false;
        if (CombatDirector.strikers.Contains(agentAI))
        {
            CombatDirector.strikers.Remove(agentAI);
            agentAI.SetUICounterActive(false);
        }
        //if (CombatDirector.strikers.Count == 0)
        //{
        //    CombatDirector.state = CombatDirectorState.Planning;
        //}
        //Attiva i rigidbody della ragdoll
        agentAI.SetRagdollKinematicRigidbody(false);
        agentAI.chestRigidbody.AddForce(((transform.position - agentAI.target.position).normalized + Vector3.up * .1f) * 70f, ForceMode.Impulse);
        StartCoroutine(StunExecution());
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        
    }

    IEnumerator StunExecution()
    {
        yield return new WaitForSeconds(hitExitTime);
        fsm.State = agentAI.recoverState;
    }
}
