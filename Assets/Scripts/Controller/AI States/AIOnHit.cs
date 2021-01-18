using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.AI;

public class AIOnHit : State
{
    public AIOnHit(GameObject go, StateMachine fsm) : base(go, fsm) { }

    AgentAI agentAI;
    Animator animator;
    NavMeshAgent agentNM;
    SphereCollider avoidanceCollider;

    int idleHash;
    int stopHash;

    //TODO target?

    public float hitExitTime = 3f;

    private void Awake()
    {
        agentAI = GetComponent<AgentAI>();
    }

    void Start()
    {        
        go = this.gameObject;
        fsm = agentAI.fsm;
        animator = agentAI.animator;
        agentNM = agentAI.agentNM;
        avoidanceCollider = agentAI.avoidanceCollider;

        SetOnHitHashParameters();
    }

    void SetOnHitHashParameters()
    {
        idleHash = agentAI.idleHash;
        stopHash = agentAI.stopHash;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        agentAI.state = AgentState.Hit;
        //agentAI.rootMotion = Vector3.zero;
        avoidanceCollider.enabled = false;
        agentAI.ResetNavMeshPath();
        agentNM.enabled = false;
        animator.enabled = false;
        if (CombatDirector.strikers.Contains(agentAI))
        {
            CombatDirector.strikers.Remove(agentAI);
            agentAI.SetUICounterActive(false);
        }
        agentAI.SetRagdollKinematicRigidbody(false);
        //agentAI.chestRigidbody.AddForce(((transform.position - agentAI.target.position).normalized + Vector3.up * .1f) * 70f, ForceMode.Impulse); //FORZA IN AGENT AI
        agentAI.chestRigidbody.AddForce(agentAI.impulseForce);
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

    //TODO controllare che i rigidbody siano in sleep!!!

    IEnumerator StunExecution()
    {
        yield return new WaitForSeconds(hitExitTime);
        animator.SetBool(stopHash, false);
        fsm.State = agentAI.recoverState;
    }
}
