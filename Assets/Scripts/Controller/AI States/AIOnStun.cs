using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using TMPro;
using UnityEngine;

public class AIOnStun : State
{
    public AIOnStun(GameObject go, StateMachine fsm) : base(go, fsm) { }

    AgentAI agentAI;
    Animator animator;

    int stunnedHash;
    int recoverHash;

    public GameObject UI_stun;

    public float stunTime = 3f;

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

        UI_stun = Instantiate(UI_stun, agentAI.animator.GetBoneTransform(HumanBodyBones.Head));
        UI_stun.SetActive(false);
    }

    void SetOnStunHashParameters()
    {
        stunnedHash = Animator.StringToHash("Stunned");
        recoverHash = Animator.StringToHash("Recover");
    }

    public override void OnEnter()
    {
        base.OnEnter();
        agentAI.state = AgentState.Stun;
        UI_stun.SetActive(true);
        animator.SetTrigger(stunnedHash);
        agentAI.ResetNavMeshPath();
        StartCoroutine(OnStunCheckIn());
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        agentAI.HandleRootMotionMovement();
    }

    IEnumerator OnStunCheckIn()
    {
        yield return new WaitForSeconds(stunTime);
        animator.SetTrigger(recoverHash);
        fsm.State = agentAI.locomotionState;
    }

    public override void OnExit()
    {
        base.OnExit();
        UI_stun.SetActive(false);
        StopAllCoroutines();
    }
}
