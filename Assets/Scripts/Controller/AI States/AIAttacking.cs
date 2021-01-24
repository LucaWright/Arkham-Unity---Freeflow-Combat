using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class AIAttacking : State
{
    public AIAttacking(GameObject go, StateMachine fsm) : base(go, fsm) { }

    AgentAI agentAI;
    Animator animator;
    NavMeshAgent agentNM;

    int idleHash;
    int attackHash;

    public float attackRange = 1.5f;
    public LayerMask attackLM; //Utile per davvero?

    public GameObject UI_counter;

    public Transform hitTransformRef;

    public UnityEvent strikeFX;


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

        idleHash = agentAI.idleHash;
        //TODO set Animator Hash 
        SetAttackingHashParameters();

        UI_counter = Instantiate(UI_counter, agentAI.animator.GetBoneTransform(HumanBodyBones.Head));
        SetUICounterActive(false);
    }
    void SetAttackingHashParameters()
    {
        attackHash = Animator.StringToHash("Attack");
    }

    public override void OnEnter()
    {
        base.OnEnter();
        agentAI.state = AgentState.Attacking;
        //agentAI.animator.SetBool("Can Attack", agentAI.canAttack);
        //agentAI.animator.ResetTrigger("Ready");
        animator.SetTrigger(attackHash);
        SetUICounterActive(true);
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
    }
    public override void OnExit()
    {
        base.OnExit();
        animator.ResetTrigger(idleHash);
        animator.ResetTrigger(attackHash);
    }

    public void OnAttackAnimationEvent()
    {
        if (agentAI.state != AgentState.Attacking) return; //Sembra che appena riattivo l'animator, vada avanti di un frame nell'animazione in corso. Ergo, rischia di attivare l'attack animation event anche se è a terra, causando il cambiamento di stato. Così dovrei prevenirlo. SOLUZIONE PEZZA! 

        RaycastHit hitInfo;
        if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out hitInfo, attackRange))
        {
            agentAI.target.GetComponent<Player>().OnHit(); //TODO prendere direttamente il component Player? E poi: fare controllo che non sia errato?
            hitTransformRef.position = hitInfo.point;
            strikeFX.Invoke();
        }
        else //Se l'attacco va a vuoto, OnHit del player non viene chiamato e il Director non torna in planning.
        {
            if (CombatDirector.strikers.Count == 1) //Per non chiamare la funzione più volte, controlla che sia l'ultimo striker rimasto
                CombatDirector.state = CombatDirectorState.Planning;
        }
        CombatDirector.strikers.Remove(agentAI);
        SetUICounterActive(false);

        agentAI.PullBack(2);
        //fsm.State = agentAI.locomotionState;
        //Lo obbliga ad andare in PullBack?
    }

    

    public void SetUICounterActive(bool boolean)
    {
        UI_counter.SetActive(boolean);
    }
}
