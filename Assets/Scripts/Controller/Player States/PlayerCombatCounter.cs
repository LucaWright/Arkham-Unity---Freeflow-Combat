using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class PlayerCombatCounter : State
{
    public PlayerCombatCounter(GameObject go, StateMachine fsm) : base(go, fsm) { }

    Player player;
    PlayerCombat combatState;

    AgentAI target;

    int counteredThugs;

    public float counterImpulseForceMagnitude = 10f;

    public float counterBeat = .75f;
    public float counterMaxRange = 2.5f;

    public float easeDistance = 1.5f;
    public Ease counterEase;
    public float counterHurtRaycastLength = 1f;
    public LayerMask counterLayerMask;

    public GameObject hitPointRef; //trasformarlo in un trasform generico del giocatore

    public UnityEvent OnCounterStartFX;
    public UnityEvent OnCounterExecutionFX;
    public UnityEvent OnCounterImpactFX;
    public UnityEvent OnCounterEndFX;

    //tenere qui:
    //Lista di strikers counterati???
    //Layermask
    //beat e durations
    //events

    //animator trigger hash?


    private void Awake()
    {
        player = GetComponent<Player>();
        combatState = GetComponent<PlayerCombat>();
    }

    void Start()
    {
        go = this.gameObject;
        fsm = player.fsm;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        //StopAllCoroutines();
        player.state = PlayerState.Counter;
        player.ResetMovementParameters();
        player.input.northButton = true; //=> Per qualche arcano motivo, durante il passaggi di stato torna a false
        counteredThugs = 0;
        if (ThugsAreInExecuting() && Vector3.Distance(this.transform.position, FirstThugPosition()) <= counterMaxRange)
        {
            StartCoroutine(Counter());
        }
        else
        {
            player.input.northButton = false;
            //false input coroutine
            fsm.State = player.locomotionState;
            return;
        }
    }

    IEnumerator Counter() //check input
    {
        yield return CounterAnticipation();
        yield return CounterExecution();
        yield return CounterRecovery();
    }

    bool ThugsAreInExecuting()
    {
        if (CombatDirector.strikers.Count > 0) //Di sicurezza, a seguito di un errore di index out of range (assurdo, se questo controllo è true)
        {
            return CombatDirector.state == CombatDirectorState.Executing;
        }
        return false;
    }

    Vector3 FirstThugPosition()
    {
        return CombatDirector.strikers.ElementAt(0).transform.position;
    }

    void CounterOnce(float lerpingTime)
    {
        if (player.input.northButton) //Check input
        {
            var thug = CombatDirector.strikers.ElementAt(counteredThugs);
            thug.SetUICounterActive(false);
            //CombatDirector.strikers.Remove(thug);
            //StartCoroutine di posizionamento?
            //thug.OnStun(); //deve fermarlo SUBITO! Pensa tranquillamente a un freeze! O altro!
            MoveCounteredThugInPosition(thug, lerpingTime);
            player.input.northButton = false;
            counteredThugs++;
            //Debug.Log("Ci sono " +CombatDirector.strikers.Count+ " stronzi nella lista. Hai premuto counter per " + counteredThugs + " volte.");
        }
    }

    void MoveCounteredThugInPosition(AgentAI thug, float lerpingTime)
    {
        Vector3 thugFinalPos = (thug.transform.position - this.transform.position).normalized * easeDistance;
        thug.transform.DOMove(this.transform.position + thugFinalPos, Mathf.Min(lerpingTime, counterBeat / 2f)).SetEase(counterEase);
    }

    //Dividiere in: ANTICIPATION, IMPACT, EXECUTION, RECOVERY    

    IEnumerator CounterAnticipation()
    {
        player.animator.SetTrigger("Counter Setup");
        OnCounterStartFX.Invoke(); //Metterla qui?

        float remainingBeat = counterBeat - (Time.time - CombatDirector.strikeStartTime);
        CounterOnce(remainingBeat);


        while (counteredThugs < CombatDirector.strikers.Count)
        {
            //remainingBeat -= Time.fixedDeltaTime;
            remainingBeat -= Time.deltaTime;
            CounterOnce(remainingBeat);
            yield return new WaitForEndOfFrame(); //il controllo input deve avvenire in update!
        }

        //yield return new WaitForFixedUpdate();

        foreach (AgentAI thug in CombatDirector.strikers)
        {
            //Positioning +
            thug.OnStun();
            //Attivare qui? counterStartFX.Invoke();
        }

        yield return new WaitForSeconds(Mathf.Max(Time.fixedDeltaTime, remainingBeat));
    }

    IEnumerator CounterExecution()
    {
        //COUNTERING

        player.animator.SetTrigger("Counter");
        OnCounterExecutionFX.Invoke();
        //Wait animation transition
        yield return new WaitForFixedUpdate();
        //Get animation transition duration and wait
        yield return new WaitForSeconds(player.animator.GetAnimatorTransitionInfo(0).duration); // + fixedDeltaTime
        //Wait for complete transition ending
        yield return new WaitForFixedUpdate();

        //Debug.Break();
        RaycastHit hitInfo;
        //Hit raycast for the duration of the counter animation
        while (!player.animator.IsInTransition(0)) //IDEALE: fino a inizio transition di uscita! Praticamente, fino all'enter del nuovo stato!
        {
            //timer += Time.fixedDeltaTime;
            Debug.DrawRay(player.rootBone.position, player.rootBone.forward * counterHurtRaycastLength, Color.red, .02f);
            if (Physics.Raycast(player.rootBone.position, player.rootBone.forward, out hitInfo, counterHurtRaycastLength, counterLayerMask))
            {
                //Debug.Break();
                //var averageSpeed = -780f * counterRange;
                //var counterImpulseForce = Vector3.Cross(Vector3.up, transform.forward) * player.mass *  averageSpeed / (counterBeat /** Time.fixedDeltaTime*/);
                var counterImpulseForce = - Vector3.Cross(Vector3.up, transform.forward) * counterImpulseForceMagnitude;
                hitInfo.transform.GetComponent<AgentAI>().OnHit(counterImpulseForce);
                hitPointRef.transform.position = hitInfo.point;
                OnCounterImpactFX.Invoke();
                yield return combatState.ImpactTimeFreeze(); //valutare!
                //yield return ImpactTimeFreeze();
            }
            yield return new WaitForFixedUpdate();
        }
        //TODO Controllare
        //Il planning inizia subito dopo finito il counter, prima di recovery.
        CombatDirector.state = CombatDirectorState.Planning;
        //Torna in Combat State per controllare nuovo input.
        fsm.State = player.combatState;
        //Intando, prosegue con Coroutine di recovery.
    }

    IEnumerator CounterRecovery()
    {
        yield return new WaitForSeconds(player.animator.GetAnimatorTransitionInfo(0).duration);
        yield return new WaitForFixedUpdate();
        while (!player.animator.IsInTransition(0))
        {
            yield return new WaitForFixedUpdate();
        }
        //Debug.Break();
        yield return new WaitForSeconds(player.animator.GetAnimatorTransitionInfo(0).duration);
        CombatDirector.state = CombatDirectorState.Planning;
        OnCounterEndFX.Invoke();
        //Debug.Break();
        fsm.State = player.locomotionState;
    }



    public override void OnExit()
    {
        base.OnExit();
        player.input.northButton = false;
        OnCounterEndFX.Invoke();
        //CombatDirector.strikers.Clear();
        //CombatDirector.state = CombatDirectorState.Planning;
        StopAllCoroutines();
    }
}
