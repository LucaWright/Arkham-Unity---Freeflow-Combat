using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerCombatStrike : State
{
    public PlayerCombatStrike(GameObject go, StateMachine fsm) : base(go, fsm) { }

    public enum StrikeVersioning { mk1, mk2, mk3}
    public StrikeVersioning version = StrikeVersioning.mk1;

    Player player;
    PlayerCombat combatState;
    CapsuleCollider capsuleCollider;

    public float strikeImpulseForceMagnitude = 10f;
    Vector3 strikeImpulseForce;

    public AgentAI target;

    //public IAnimatedAction iAction;
    public AnimatedAction action;
    public IEnumerator Anticipation;

    public float fow = 45f;

    public float strikeSpeed = 3f;
    public float strikeBeat = .75f;
    public Ease strikeEase;
    public LayerMask strikeLayerMask;
    public float stoppingDistance = .75f;

    public GameObject hitPointRef; //trasformarlo in un trasform generico del giocatore
    public UnityEvent OnStrikeStartFX;
    public UnityEvent OnStrikeImpactFX;
    public UnityEvent OnStrikeEndFX;

    private void Awake()
    {
        player = GetComponent<Player>();
        combatState = GetComponent<PlayerCombat>();
        capsuleCollider = GetComponent<CapsuleCollider>();
    }

    void Start()
    {
        go = this.gameObject;
        fsm = player.fsm;
        action.SetAction(this.transform);
        Anticipation = action.Anticipation();
    }

    public override void OnEnter()
    {
        base.OnEnter();
        player.state = PlayerState.Strike;
        player.ResetMovementParameters();
        player.animator.ResetTrigger("Strike Ender");
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

    //FOR EDITOR ONLY!
    public Vector3 DirFromAngle(float angle)
    {
        if (Application.isPlaying && player.movementVector != Vector3.zero)
        {
            return Quaternion.AngleAxis(angle, Vector3.up) * player.movementVector.normalized;
        }
        return Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
    }

    bool FindTarget()
    {
        RaycastHit hitInfo;
        //SphereCastAll per dare la priorità a un eventuale avversario che non è a terra.
        if (Physics.SphereCast(transform.position + Vector3.up, .25f, player.movementVector, out hitInfo, 20f, strikeLayerMask))
        {
            target = hitInfo.transform.GetComponent<AgentAI>();
            //Debug.Log("Now we are striking " + target.gameObject.name);
            return true;
        }
        else
        {
            Collider thugCollider = null;
            float refAngle = fow / 2f;

            capsuleCollider.enabled = false;
            Collider[] thugColliders = Physics.OverlapSphere(transform.position, 20f, strikeLayerMask); //decidere un raggio massimo, in teoria in base al free flow state
            capsuleCollider.enabled = true;
            foreach (Collider collider in thugColliders)
            {
                float angle = Vector3.Angle(player.movementVector, collider.transform.position - transform.position);

                if (angle < refAngle)
                {
                    thugCollider = collider;
                    refAngle = angle;
                }
            }

            if (thugCollider != null)
            {
                //Debug.Log(thugCollider.gameObject.name); //sospetto che hitti contro sé stesso
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

    //Old method
    IEnumerator StrikeAnticipation()
    {
        switch (version)
        {
            case StrikeVersioning.mk1:
                player.animator.SetTrigger("Strike");
                OnStrikeStartFX.Invoke();

                float easedValue = 0;
                float attackPercentage = 0;

                Vector3 startingAttackPosition = transform.position;
                Vector3 vecToTarget = target.transform.position - player.transform.position;
                Vector3 separator = -vecToTarget.normalized * stoppingDistance;

                //IDEA: Gestire ritmo\distanza tramite animation curve!

                //strikeImpulseForce = player.mass * (vecToTarget + separator) / (strikeBeat /** Time.fixedDeltaTime*/); //andrebbe puntata un po' in basso
                strikeImpulseForce = (vecToTarget + separator) * strikeImpulseForceMagnitude;

                //Handle rotation
                transform.rotation = Quaternion.LookRotation(vecToTarget, Vector3.up);

                while (attackPercentage / strikeBeat < 1)
                {
                    attackPercentage += Time.fixedDeltaTime;
                    easedValue = DOVirtual.EasedValue(0, 1, Mathf.Clamp01(attackPercentage / strikeBeat), strikeEase);
                    transform.position = Vector3.Lerp(startingAttackPosition, target.transform.position + separator, easedValue);
                    //transform.Translate(Vector3.Lerp(startingAttackPosition, target.transform.position + separator, easedValue));
                    yield return new WaitForFixedUpdate();
                }
                yield break;

            case StrikeVersioning.mk2:
                player.animator.SetTrigger("Strike");
                //Target => Stop (specie se è striker) //DEVE FORZARLO IN IDLE!

                easedValue = 0;
                attackPercentage = 0;

                startingAttackPosition = transform.position;
                vecToTarget = target.transform.position - player.transform.position;
                separator = -vecToTarget.normalized * stoppingDistance;

                //Handle rotation
                transform.rotation = Quaternion.LookRotation(vecToTarget, Vector3.up);

                Vector3 distanceVector = vecToTarget + separator;
                Time.timeScale = Mathf.Clamp(distanceVector.magnitude / strikeSpeed, .3f, 1f);

                player.animator.updateMode = AnimatorUpdateMode.Normal;

                while (attackPercentage / strikeBeat < 1)
                {
                    //attackPercentage += Time.fixedUnscaledDeltaTime;
                    attackPercentage += Time.unscaledDeltaTime;
                    easedValue = DOVirtual.EasedValue(0, 1, Mathf.Clamp01(attackPercentage / strikeBeat), strikeEase);

                    //Creare un lerp tra oldPos e newPos che duri quanto FIXEDDELTATIME. Questo dovrebbe evitare quegli "scattini fastidiosi durante l'update)

                    transform.position = Vector3.Lerp(startingAttackPosition, target.transform.position + separator, easedValue);
                    //yield return new WaitForFixedUpdate();
                    yield return new WaitForEndOfFrame();
                }

                player.animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
                yield break;
            case StrikeVersioning.mk3:
                break;
            default:
                break;
        }        
    }

    ////NewMethod
    //IEnumerator StrikeAnticipation()
    //{
    //    player.animator.SetTrigger("Strike");
    //    //Target => Stop (specie se è striker) //DEVE FORZARLO IN IDLE!

    //    float easedValue = 0;
    //    float attackPercentage = 0;

    //    Vector3 startingAttackPosition = transform.position;
    //    Vector3 vecToTarget = target.transform.position - player.transform.position;
    //    Vector3 separator = -vecToTarget.normalized * stoppingDistance;

    //    //Handle rotation
    //    transform.rotation = Quaternion.LookRotation(vecToTarget, Vector3.up);

    //    Vector3 distanceVector = vecToTarget + separator;
    //    Time.timeScale = Mathf.Clamp(distanceVector.magnitude / strikeSpeed, .3f, 1f);

    //    player.animator.updateMode = AnimatorUpdateMode.Normal; 

    //    while (attackPercentage / strikeBeat < 1)
    //    {
    //        //attackPercentage += Time.fixedUnscaledDeltaTime;
    //        attackPercentage += Time.unscaledDeltaTime;
    //        easedValue = DOVirtual.EasedValue(0, 1, Mathf.Clamp01(attackPercentage / strikeBeat), strikeEase);

    //        //Creare un lerp tra oldPos e newPos che duri quanto FIXEDDELTATIME. Questo dovrebbe evitare quegli "scattini fastidiosi durante l'update)

    //        transform.position = Vector3.Lerp(startingAttackPosition, target.transform.position + separator, easedValue);
    //        //yield return new WaitForFixedUpdate();
    //        yield return new WaitForEndOfFrame();
    //    }

    //    player.animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
    //}

    IEnumerator StrikeImpact()
    {
        //STRIKE IMPACT
        player.animator.SetTrigger("Strike Ender");
        hitPointRef.transform.position = target.chestTransf.position; //compromesso

        target.OnHit(strikeImpulseForce);
        OnStrikeImpactFX.Invoke();
        yield return combatState.ImpactTimeFreeze(); //dovrebbe essere presa sempre dagli event

        player.input.westButton = false; //farglielo fare all'input manager? 
        player.input.northButton = false;

        OnStrikeEndFX.Invoke();

        fsm.State = player.combatState;
    }

    //TODO
    //Recovery che riporti in Locomotion

    public override void OnExit()
    {
        base.OnExit();
        player.input.northButton = false;
        Time.timeScale = 1;
        OnStrikeEndFX.Invoke();
        
        //TODO
        //Una volta che c'è la recovery, queste devono essere messe (ad eccezione di stop all coroutines) a fine impact
        //Maaaaa... Mettere StopAllCoroutines come funzione base di State Enter PRIMA del passaggio di stato?
        StopAllCoroutines();
        if (CombatDirector.strikers.Count > 0) return;
        CombatDirector.state = CombatDirectorState.Planning;
    }
}
