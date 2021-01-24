using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class AIRetrat : State
{
    public AIRetrat(GameObject go, StateMachine fsm) : base(go, fsm) { }

    public enum Context { Backstep, Sidestep }
    public Context contextMap = Context.Backstep;

    AgentAI agentAI;
    Animator animator;
    SphereCollider avoidanceCollider;

    //int idleHash;

    public Vector2 everyMinMaxSeconds;
    public float minMovementMagnitude = .7f;
    public float retreatTriggerAngleRange = 30f;

    public LayerMask avoidanceMask;

    int movingForwardHash;
    int movingRightHash;

    public float[] interestMapWeights;
    public Vector3[] contextVectors;
    public float[] dangerMap;
    public float[] inverseInterestMap;

    public Vector3 desiredDir;

    private void Awake()
    {
        agentAI = GetComponent<AgentAI>();
    }

    void Start()
    {
        go = this.gameObject;
        fsm = agentAI.fsm;
        animator = agentAI.animator;
        avoidanceCollider = agentAI.avoidanceCollider;

        movingForwardHash = Animator.StringToHash("Forward");
        movingRightHash = Animator.StringToHash("Right");
    }

    public override void OnEnter()
    {
        base.OnEnter();
        agentAI.state = AgentState.Retreat;
        desiredDir = Vector3.back;
        //StartCoroutine(RetreatCheckIn());
    }

    public override void OnUpdate()
    {
        base.OnUpdate();        
    }

    //IEnumerator RetreatCheckIn()
    //{
    //    while (!animator.IsInTransition(0))
    //    {
    //        yield return new WaitForFixedUpdate();
    //    }
    //    yield return new WaitForSeconds(animator.GetAnimatorTransitionInfo(0).duration);
    //    StartCoroutine(RetreatCheckOut());
    //}

    //IEnumerator RetreatCheckOut()
    //{
    //    while (agentAI.currentLine < agentAI.destinationLine)
    //    {
    //        yield return new WaitForFixedUpdate();
    //    }
    //    //StartCoroutine(agentAI.BackToIdle());
    //    agentAI.BackToIdle();
    //}

    public override void OnFixedUpdate() //trasformalo in un banalissimo cooldown?
    {
        base.OnFixedUpdate();
        agentAI.HandleRootMotionMovement();
        agentAI.HandleRootMotionRotation();
        UpdateAnimatorLocomotionParameters();
        switch (CombatDirector.state)
        {
            case CombatDirectorState.Planning:
                if (agentAI.currentLine <= 1) return;
                fsm.State = agentAI.locomotionState; 
                break;
            case CombatDirectorState.Dispatching:
                break;
            case CombatDirectorState.Executing:
                //Se ha raggiunto destinazione, deve comunque tornare in locomotion per gestire i suoi avoidance! Se si riavvicina un po'... chissene, è alla linea 2. TODO check
                if (agentAI.currentLine == agentAI.destinationLine) //ma così hanno le convulsioni rimbalzandosi da uno stato all'altro! Ci vuole un'altra avoidance che non contempli la forward!
                {
                    fsm.State = agentAI.locomotionState;
                }
                break;
        }
    }

    //FUNZIONE COMUNICAZIONE CON AI DIETRO DI SE'

    public override void OnExit()
    {
        base.OnExit();
        //animator.ResetTrigger(idleHash);
        StopAllCoroutines();
    }

    void SetContextMaps()
    {
        switch (contextMap) //Starebbero meglio in uno Scriptable, ma non ho né il tempo né la voglia di starci dietro. Magari in futuro e se mi servirà un sistema di IA sismile
        {
            case Context.Backstep:
                contextVectors = new Vector3[]
                {
                    Vector3.forward
                };
                interestMapWeights = new float[]
                {
                    1f      ,
                };
                dangerMap = new float[contextVectors.Length];
                inverseInterestMap = new float[contextVectors.Length];
                break;
            case Context.Sidestep:
                contextVectors = new Vector3[]
                {
                    Vector3.right       ,
                    Vector3.left

                };
                interestMapWeights = new float[]
                {
                    .7f     ,
                    .7f
                };
                dangerMap = new float[contextVectors.Length];
                inverseInterestMap = new float[contextVectors.Length];
                break;
            default:
                break;
        }
    }

    IEnumerator LocomotionCheck()
    {
        var everyXseconds = Random.Range(everyMinMaxSeconds.x, everyMinMaxSeconds.y);
        yield return new WaitForSeconds(everyXseconds);
        agentAI.destinationLine = 1;
        ResetAllAvoidanceMaps();
        CheckSurroundings();
        StartCoroutine(LocomotionCheck());
    }

    void ResetAllAvoidanceMaps()
    {
        for (int i = 0; i < dangerMap.Length; i++)
        {
            dangerMap[i] = 0;
        }
        for (int i = 0; i < inverseInterestMap.Length; i++)
        {
            inverseInterestMap[i] = 0;
        }
    }

    public void CheckSurroundings()
    {
        avoidanceCollider.enabled = false;
        Collider[] characterColliders = Physics.OverlapSphere(avoidanceCollider.transform.position, CombatDirector.DistanceInfo.LineToLineDistance, avoidanceMask);
        avoidanceCollider.enabled = true;

        int characterArrayLength = characterColliders.Length;
        if (characterArrayLength > 0)
        {
            for (int i = 0; i < characterArrayLength; i++)
            {
                Vector3 distanceVector = (characterColliders[i].transform.position - transform.position);
                if (CheckIfOverride(distanceVector)) return;
                UpdateDangerMap(distanceVector);
                Debug.DrawRay(transform.position + Vector3.up, distanceVector, Color.red, .02f);
            }
        }
        EvaluateMovement();
    }

    bool CheckIfOverride(Vector3 distanceVector)
    {
        float dot = Vector3.Dot(distanceVector, WorldToLocalDir(contextVectors[0]));
        if (distanceVector.sqrMagnitude <= Mathf.Pow(CombatDirector.DistanceInfo.LineToLineDistance, 2) && dot > Mathf.Cos(retreatTriggerAngleRange)) //Vale anche per currentLine == 0; TODO: mettere parametro su inspector, come angolo (cos angle clampato tra 0 e 90)
        {
            desiredDir = Vector3.back;
            agentAI.destinationLine = agentAI.currentLine + 1;
            return true;
        }
        return false;
    }

    void UpdateDangerMap(Vector3 distanceVector)
    {
        for (int i = 0; i < contextVectors.Length; i++)
        {
            float dot = Vector3.Dot(distanceVector.normalized, WorldToLocalDir(contextVectors[i]));
            if (dot > dangerMap[i])
            {
                dangerMap[i] = dot;
            }
        }
    }

    void EvaluateMovement() //WITH INVERSE INTEREST MAP!!!
    {

        float interest = 0f;
        int index = 0;

        for (int i = 0; i < dangerMap.Length; i++)
        {
            //Update Interest Map (weighed) //INVERSE!!!
            inverseInterestMap[i] = dangerMap[i] * interestMapWeights[i];

            //And check for the highest interest
            if (inverseInterestMap[i] > interest)
            {
                interest = inverseInterestMap[i];
                index = i;
            }
        }
        Debug.DrawRay(transform.position + Vector3.up, WorldToLocalDir( - contextVectors[index]) * inverseInterestMap[index], Color.blue, .02f);
        desiredDir = ( - contextVectors[index] * inverseInterestMap[index]).sqrMagnitude >= Mathf.Pow(minMovementMagnitude, 2) ?
                     - contextVectors[index] : Vector3.zero;
    }
    //DIFFERENZE!
    //La inverse context map è UGUALE A DANGER * I PESI
    //IL VETTORE VINCITORE VA MOLTIPLICATO PER -1
    //SE DANGER MAP è 0... NON È INTERESSATA A MUOVERSI!

    void UpdateAnimatorLocomotionParameters()
    {
        if (desiredDir != Vector3.zero && agentAI.currentLine < agentAI.destinationLine)
        {
            animator.SetFloat(movingForwardHash, desiredDir.z, .1f, Time.fixedDeltaTime);
            animator.SetFloat(movingRightHash, desiredDir.x, .1f, Time.fixedDeltaTime);
        }
        else //Deceleration
        {
            animator.SetFloat(movingForwardHash, 0, .25f, Time.fixedDeltaTime);
            animator.SetFloat(movingRightHash, 0, .25f, Time.fixedDeltaTime);
        }
    }

    Vector3 WorldToLocalDir(Vector3 direction)
    {
        return direction.z * transform.forward + direction.x * transform.right + direction.y * transform.up;
    }
}
