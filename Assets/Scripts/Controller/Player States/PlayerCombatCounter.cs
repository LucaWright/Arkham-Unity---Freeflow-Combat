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

    public float counterRay = 2.5f;

    public float counterBeat = .75f;
    public Ease counterEase;
    public LayerMask counterLayerMask;
    public float stoppingDistance = 1.5f;
    public float counterattackRay = 2f; //NON PUO' ESSERE INFERIORE ALLA STOPPING DISTANCE!

    public GameObject hitPointRef; //trasformarlo in un trasform generico del giocatore

    public UnityEvent counterStartFX;
    public UnityEvent counterFX;

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
        player.ResetMovementParameters();
        if (ThugsAreInExecuting() &&
            Vector3.Distance(this.transform.position, FirstThugPosition()) <= counterattackRay)
        {
            player.state = PlayerState.Counter;
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

    bool ThugsAreInExecuting()
    {
        return CombatDirector.state == CombatDirectorState.Executing;
    }

    Vector3 FirstThugPosition()
    {
        return CombatDirector.strikers.ElementAt(0).transform.position;
    }

    void CounterOnce()
    {
        var thug = CombatDirector.strikers.ElementAt(0);
        thug.SetUICounterActive(false);
        CombatDirector.strikers.Remove(thug);
        thug.OnStun(); //deve fermarlo SUBITO! Pensa tranquillamente a un freeze! O altro!
        player.input.northButton = false;

        //Do move in posizione di countering, con do ease e stopping distance
    }

    //Dividiere in: ANTICIPATION, IMPACT, EXECUTION, RECOVERY

    IEnumerator Counter() //check input
    {
        yield return CounterAnticipation();
        yield return CounterExecution();
        yield return CounterRecovery();        
    }

    IEnumerator CounterAnticipation()
    {
        player.animator.SetTrigger("Counter Setup");
        counterStartFX.Invoke();

        //player.batFX.Play();

        CounterOnce();
        float remainingBeat = counterBeat; //In realtà, dovrebbe partire dall'inizio dell'attacco. Vedere se fare un beat master

        //Continue only when all strikers are countered
        while (CombatDirector.strikers.Count != 0)
        {
            remainingBeat -= Time.fixedDeltaTime;
            if (player.input.northButton) //Check input
            {
                CounterOnce();
            }
            yield return new WaitForFixedUpdate();
        }
        //End setup
        yield return new WaitForSeconds(remainingBeat);
    }

    IEnumerator CounterExecution()
    {
        //COUNTERING
        player.animator.SetTrigger("Counter");
        //Wait animation transition
        yield return new WaitForFixedUpdate();

        //Debug.Log(animator.GetAnimatorTransitionInfo(0).duration);
        //Debug.Break();

        //Get animation transition duration and wait
        yield return new WaitForSeconds(player.animator.GetAnimatorTransitionInfo(0).duration); // + fixedDeltaTime
        //Wait for complete transition ending
        yield return new WaitForFixedUpdate();
        //Debug.Log(animator.GetCurrentAnimatorClipInfo(0)[0].clip.name);
        //Debug.Log(animator.GetCurrentAnimatorClipInfo(0)[0].clip.length);
        //Debug.Log(animator.GetNextAnimatorStateInfo(0).length);

        //float timer = 0;

        //Debug.Break();
        RaycastHit hitInfo;
        //Hit raycast for the duration of the counter animation
        while (!player.animator.IsInTransition(0)) //IDEALE: fino a inizio transition di uscita! Praticamente, fino all'enter del nuovo stato!
        {
            //timer += Time.fixedDeltaTime;
            Debug.DrawRay(player.rootBone.position, player.rootBone.forward * counterattackRay, Color.red, .02f);
            if (Physics.Raycast(player.rootBone.position, player.rootBone.forward, out hitInfo, counterattackRay, counterLayerMask))
            {
                hitInfo.transform.root.GetComponent<AgentAI>().OnHit(); //Problema root
                hitPointRef.transform.position = hitInfo.point;
                counterFX.Invoke();

                yield return combatState.ImpactTimeFreeze(); //valutare!
                //yield return ImpactTimeFreeze();
            }
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator CounterRecovery()
    {
        yield return new WaitForSeconds(player.animator.GetAnimatorTransitionInfo(0).duration); //+ fixedDeltaTime
        yield return new WaitForFixedUpdate();
        while (!player.animator.IsInTransition(0))
        {
            yield return new WaitForFixedUpdate();
        }
        //Debug.Break();
        yield return new WaitForSeconds(player.animator.GetAnimatorTransitionInfo(0).duration);
        //Debug.Break();
        fsm.State = player.locomotionState;
    }



    public override void OnExit()
    {
        base.OnExit();
        StopAllCoroutines();
    }
}
