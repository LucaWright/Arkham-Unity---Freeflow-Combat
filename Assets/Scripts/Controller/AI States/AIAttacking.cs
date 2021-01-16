using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.Events;

public class AIAttacking : State
{
    public AIAttacking(GameObject go, StateMachine fsm) : base(go, fsm) { }

    AgentAI agentAI;

    public float attackRange = 1.5f;
    public LayerMask attackLM; //Utile per davvero?

    public GameObject UI_counter;

    public Transform hitTransformRef;

    public UnityEvent strikeFX;

    //UI counter
    //Gestione animazioni?
    //Gestione DO MOVE per posizionamento perfetto
    //Attack range?? Ma come passo la stopping distance in dispatch? Meglio tenerla in GLOBAL

    private void Awake()
    {
        agentAI = GetComponent<AgentAI>();
    }

    void Start()
    {        
        go = this.gameObject;
        fsm = agentAI.fsm;
        UI_counter = Instantiate(UI_counter, agentAI.animator.GetBoneTransform(HumanBodyBones.Head));
        SetUICounterActive(false);
    }

    public override void OnEnter()
    {
        base.OnEnter();
        agentAI.state = AgentState.Attacking;
        agentAI.canAttack = false; //Può farlo in uscita del dispatching
        agentAI.isCandidate = false; //Idem come sopra

        //reset trigger di sicurezza
        agentAI.animator.SetBool("Can Attack", agentAI.canAttack);
        agentAI.animator.ResetTrigger("Ready");

        //Funzioni di attacco
        agentAI.animator.SetTrigger("Attack"); //Può tranquillamente essere LOCAL
        SetUICounterActive(true); //LOCAL? Sia il settaggio che altro? Ci potrebbe stare. Gestirebbe la sua propria UI, magari in base al tipo di attacco.
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        //Fino a prima dell'attacco
        agentAI.HandleNavMeshMovement();
        agentAI.HandleNavMeshRotation();
    }

    public void OnAttackAnimationEvent()
    {
        agentAI.agentNM.ResetPath();

        RaycastHit hitInfo;
        if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out hitInfo, agentAI.attackRange))
        {
            agentAI.target.GetComponent<Player>().OnHit();
            hitTransformRef.position = hitInfo.point;
            strikeFX.Invoke();
        }
        SetUICounterActive(false);
        //agentAI.BackToIdle(); //Ce n'è davvero bisogno? Va in Idle (animzione) automaticamente. A MENO CHE NON INSERISCA STATO VUOTO. IN QUEL CASO, SERVE ECCOME!
        //fsm.State = agentAI.idleState;
        StartCoroutine(agentAI.BackToIdle1());
    }

    public override void OnExit()
    {
        base.OnExit();
        agentAI.animator.ResetTrigger(agentAI.idleHash);
    }

    public void SetUICounterActive(bool boolean)
    {
        UI_counter.SetActive(boolean);
    }
}
