using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.AI;

public class AIIdle : State
{
    public AIIdle(GameObject go, StateMachine fsm) : base(go, fsm) { }

    AgentAI agentAI;

    Animator animator;
    NavMeshAgent agentNM;
    SphereCollider avoidanceCollider;

    int idleHash;
    int movingForwardHash;
    int movingBackwardHash;
    int movingRightHash;
    int movingLeftHash;

    public float positioningCheckTime = 1.5f;

    DistanceHandler distanceInfo;
    public LayerMask lineOfSightLM;
    public LayerMask posEvaluationLM;

    public const float referenceAngle = 45f;
    public const float straightAngle = 180f;

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

        SetIldeHashParamters();

        distanceInfo = CombatDirector.DistanceInfo;
    }

    void SetIldeHashParamters()
    {
        idleHash = agentAI.idleHash;
        movingForwardHash = agentAI.movingForwardHash;
        movingBackwardHash = agentAI.movingBackwardHash;
        movingRightHash = agentAI.movingRightHash;
        movingLeftHash = agentAI.movingLeftHash;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        agentAI.state = AgentState.Idle;
        //TODO HIGH PRIORITY: CheckIn
        //CheckIn aspetta che la transizione all'animazione di Idle finisca prima di iniziare le sue effettive funzioni. NON PRIMA
        //TUTTI GLI STATI DOVREBBERO AVERE UN CHECK IN, PERCHÉ È RESPONSABILITA' LORO REGOLARE LE PROPRIE LOGICHE INTERNE!

        //StartCoroutine(IdleCheckOut());
        StartCoroutine(IdleCheckIn()); //NON FUNZIONA SE ENTRA DA SE' STESSA!
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        agentAI.HandleRootMotionMovement();
        agentAI.HandleRootMotionRotation();
        if (!agentAI.IsValidLine())
        {
            StopAllCoroutines();
            return; //Serve ancora? Davvero? Dato che Player è Character, conta anche lui nel calcolo dei soorundings. Anche sé... Quella funzione non verrebbe mai chiamata, se c'è via libera verso il player. Meglio tenerla.
        }
            
        //TODO (test):
        switch (CombatDirector.state)
        {
            case CombatDirectorState.Planning:
                break;
            case CombatDirectorState.Dispatching:
                if (agentAI.currentLine == 1) //Così, però viene chiamata continuamente finché non esce da dispatching! TODO!
                {
                    if (CombatDirector.strikers.Contains(agentAI)) return; //TODO: da valutare! Teneral o meno? Sperimenta col nuovo sistema!
                    //StartCoroutine(agentAI.PullBack(2));
                    agentAI.PullBack(2);
                }
                break;
            case CombatDirectorState.Executing:
                break;
            default:
                break;
        }
    }

    //TODO
    //Funzione emergenza OnCollisionEnter

    public override void OnExit()
    {
        base.OnExit(); //In teoria, tutti i reset devono restare
        //animator.ResetTrigger(idleHash);
        //animator.ResetTrigger(movingForwardHash);
        //animator.ResetTrigger(movingBackwardHash); //TODO TEST: NON RESETTARE?
        //animator.ResetTrigger(movingRightHash);
        //animator.ResetTrigger(movingLeftHash);
        StopAllCoroutines();
    }

    IEnumerator IdleCheckIn()
    {
        yield return new WaitForEndOfFrame();
        StartCoroutine(IdleCheckInFromSelf());
        while (!animator.IsInTransition(0))
        {
            yield return new WaitForFixedUpdate();
        }
        yield return new WaitForSeconds(animator.GetAnimatorTransitionInfo(0).duration);
        StartCoroutine(IdleCheckOut());
    }
    IEnumerator IdleCheckInFromSelf()
    {
        var clipLength = animator.GetCurrentAnimatorClipInfo(0)[0].clip.length;
        //yield return new WaitForSeconds(clipLength);
        yield return new WaitForSeconds(3f);
        StopCoroutine(IdleCheckIn());
        StartCoroutine(IdleCheckOut());
    }

    IEnumerator IdleCheckOut()
    {
        yield return new WaitForSeconds(positioningCheckTime);
        animator.ResetTrigger(idleHash); //Security Check
        if (agentAI.HasGreenLightToTheTarget())
        {
            EvaluatePushForward();
            yield break;
        }
        agentAI.CheckSurroundings();
        StartCoroutine(IdleCheckOut());
    }

    void EvaluatePushForward() //ATTENZIONE! Se richiama sé stessa, non esce più dal checkin!
    {
        if (agentAI.currentLine == 1) //Con questo controllo, continua il ciclo
        {
            StartCoroutine(IdleCheckOut());
            return;
        }
        //StartCoroutine(agentAI.PushForward(1));
        agentAI.PushForward(1);
    }

    private void OnDrawGizmosSelected()
    {
        if (agentAI != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position + Vector3.up, distanceInfo.LineToLineDistance);
        }
        //Debug
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + Vector3.up, 2f);
    }
}
