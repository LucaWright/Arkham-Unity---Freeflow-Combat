using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerCombatStrike : State
{
    public PlayerCombatStrike(GameObject go, StateMachine fsm) : base(go, fsm) { }

    Player player;
    PlayerCombat combatState;

    public AgentAI target;

    public float fow = 45f;

    public float strikeBeat = .75f;
    public Ease strikeEase;
    public LayerMask strikeLayerMask;
    public float stoppingDistance = .75f;

    public GameObject hitPointRef; //trasformarlo in un trasform generico del giocatore
    public UnityEvent strikeFX;


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
        player.state = PlayerState.Strike;
        player.ResetMovementParameters();
        if (FindTarget())
        {
            StartCoroutine(Strike());
        }
        else
        {
            //start coroutine false feedback
            player.input.westButton = false;
            player.fsm.State = player.locomotionState;
        }
    }

    bool FindTarget() //TROVARE ERRORE!
    {
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position + Vector3.up, .75f, player.movementVector, out hitInfo, 20f, strikeLayerMask)) //cambia in spheraCastAll. Deve dare la priorità a chi non è a terra.
        {
            target = hitInfo.transform.GetComponent<AgentAI>();
            return true;
        }
        else
        {
            Collider thugCollider = null;
            float refAngle = fow / 2f;

            Collider[] thugColliders = Physics.OverlapSphere(transform.position, 20f, strikeLayerMask); //decidere un raggio massimo, in teoria in base al free flow state
            foreach (Collider collider in thugColliders)
            {
                float angle = Vector3.Angle(player.movementVector, collider.transform.position);

                if (angle < refAngle)
                {
                    thugCollider = collider;
                    refAngle = angle;
                }
            }

            if (thugCollider != null)
            {
                Debug.Log(thugCollider.gameObject.name); //sospetto che hitti contro sé stesso
                target = thugCollider.transform.GetComponent<AgentAI>();
                return true;
            }
        }    
        return false;
    }

    IEnumerator Strike()
    {
        yield return StrikeAnticipation();
        yield return StrikeImpact();        
    }

    IEnumerator StrikeAnticipation()
    {
        player.animator.SetTrigger("Strike");
        //Target => Stop (specie se è striker) //DEVE FORZARLO IN IDLE!

        float easedValue = 0;
        float attackPercentage = 0;

        Vector3 startingAttackPosition = transform.position;
        Vector3 vecToTarget = target.transform.position - player.transform.position;
        Vector3 separator = -vecToTarget.normalized * stoppingDistance;

        //Handle rotation
        transform.rotation = Quaternion.LookRotation(vecToTarget, Vector3.up);

        while (attackPercentage / strikeBeat < 1)
        {
            attackPercentage += Time.fixedDeltaTime;
            easedValue = DOVirtual.EasedValue(0, 1, Mathf.Clamp01(attackPercentage / strikeBeat), strikeEase);
            transform.position = Vector3.Lerp(startingAttackPosition, target.transform.position + separator, easedValue);
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator StrikeImpact()
    {
        //STRIKE IMPACT
        player.animator.SetTrigger("Strike Ender");
        hitPointRef.transform.position = target.chestTransf.position; //compromesso

        target.OnHit();
        strikeFX.Invoke();
        yield return combatState.ImpactTimeFreeze(); //dovrebbe essere presa sempre dagli event

        player.input.westButton = false; //farglielo fare all'input manager? 
        player.input.northButton = false;

        fsm.State = player.combatState;
    }

    public override void OnExit()
    {
        base.OnExit();
        StopAllCoroutines();
    }
}
