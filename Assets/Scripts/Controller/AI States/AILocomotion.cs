using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AILocomotion : State
{
    public AILocomotion(GameObject go, StateMachine fsm) : base (go, fsm) { }

    public enum Context { PushForward, PullBack, Sidestep}

    public Context contextMap = Context.PushForward;

    AgentAI agentAI;
    Animator animator;
    SphereCollider avoidanceCollider;

    int movingForwardHash;
    int movingRightHash;

    public Vector2 everyMinMaxSeconds;

    public LayerMask avoidanceMask;
    public float minMovementMagnitude = .35f;

    [Range(0, 90)]
    public float retreatTriggerAngleRange = 30f;

    public float[] interestMapWeights;
    public Vector3[] contextVectors;
    public float[] dangerMap;
    public float[] interestMap;

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

        contextVectors = new Vector3[]
        {
            Vector3.forward     , //in retreat *= -1
            Vector3.right       ,
            Vector3.left        /*,
            Vector3.back*/
        };
        interestMapWeights = new float[]
        {
            1f      ,
            .7f     ,
            .7f     ,
            .5f
        };

        dangerMap = new float[contextVectors.Length];
        interestMap = new float[contextVectors.Length];

        movingForwardHash = Animator.StringToHash("Forward");
        movingRightHash = Animator.StringToHash("Right");
    }

    public override void OnEnter()
    {
        base.OnEnter();
        agentAI.state = AgentState.Locomotion;
        //TODO: attiva trigger di sicurezza STOP che resetta all'uscita?
        StartCoroutine(LocomotionCheck());
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        agentAI.IsValidLine();
        UpdateAnimatorLocomotionParameters();
        agentAI.HandleRootMotionMovement();
        agentAI.HandleRootMotionRotation();
        switch (CombatDirector.state) //REGOLARE FLUSSO. Usa stato di retreat. Ritornano in locomotion alla fine col ritorno a planning.
        {
            case CombatDirectorState.Planning:
                if(contextMap != Context.PushForward)
                {
                    contextMap = Context.PushForward;
                }
                break;
            case CombatDirectorState.Dispatching:
                if (contextMap != Context.PushForward)
                {
                    contextMap = Context.PushForward;
                }
                break;
            case CombatDirectorState.Executing:
                if (!CombatDirector.strikers.Contains(agentAI) && agentAI.currentLine <= 1)
                {
                    contextMap = Context.PullBack;
                }
                else
                {
                    contextMap = Context.Sidestep;
                }
                break;
            default:
                break;
        }

        if (!agentAI.agentNM.isActiveAndEnabled)
        {
            Debug.Break();
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        StopAllCoroutines();
    }

    IEnumerator LocomotionCheck()
    {
        var everyXseconds = Random.Range(everyMinMaxSeconds.x, everyMinMaxSeconds.y);
        yield return new WaitForSeconds(everyXseconds);
        agentAI.destinationLine = 1;
        //ResetAllAvoidanceMaps();
        SetContextMaps();
        CheckSurroundings();             
        StartCoroutine(LocomotionCheck());
    }

    void SetContextMaps()
    {
        switch (contextMap) //Starebbero meglio in uno Scriptable, ma non ho né il tempo né la voglia di starci dietro. Magari in futuro e se mi servirà un sistema di IA sismile
        {
            case Context.PushForward:
                contextVectors = new Vector3[]
                {
                    Vector3.forward     , //in retreat *= -1
                    Vector3.right       ,
                    Vector3.left        /*,
                    Vector3.back*/
                };
                interestMapWeights = new float[]
                {
                    1f      ,
                    .7f     ,
                    .7f     ,
                    .5f
                };
                dangerMap = new float[contextVectors.Length];
                interestMap = new float[contextVectors.Length];
                break;
            case Context.PullBack:
                contextVectors = new Vector3[]
                {
                    Vector3.forward
                };
                interestMapWeights = new float[]
                {
                    1f      ,
                };
                dangerMap = new float[contextVectors.Length]; //Usa interestMap invertita!
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
                break;
        }
    }

    void ResetAllAvoidanceMaps()
    {
        for (int i = 0; i < dangerMap.Length; i++)
        {
            dangerMap[i] = 0;
        }
        for (int i = 0; i < interestMap.Length; i++)
        {
            interestMap[i] = 0;
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
                if(contextMap != Context.PullBack && CheckIfOverride(distanceVector)) return; //ANCHE QUESTO CAMBIA CON ENUM!!! <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                UpdateDangerMap(distanceVector);
                Debug.DrawRay(transform.position + Vector3.up, distanceVector, Color.red, .02f);
            }
        }
        EvaluateMovement();
    }

    bool CheckIfOverride(Vector3 distanceVector)
    {        
        float dot = Vector3.Dot(distanceVector, transform.forward);
        if (distanceVector.sqrMagnitude <= Mathf.Pow(CombatDirector.DistanceInfo.LineToLineDistance, 2) && dot > Mathf.Cos(retreatTriggerAngleRange)) //Vale anche per currentLine == 0; TODO: mettere parametro su inspector, come angolo (cos angle clampato tra 0 e 90)
        {
            desiredDir = Vector3.back;
            agentAI.destinationLine = agentAI.currentLine + 1;
            return true;
        }
        return false;
    }

    //Inverse Danger Map!

    void UpdateDangerMap(Vector3 distanceVector)
    {        
        for (int i = 0; i < contextVectors.Length; i++) //ALTRI CONTEXT VECTOR SE ALTRA SITUA!
        {
            float dot = Vector3.Dot(distanceVector.normalized, WorldToLocalDir(contextVectors[i]));
            if (dot > dangerMap[i])
            {
                dangerMap[i] = dot;
            }
        }
    }

    void EvaluateMovement() //SE INVERSE: INTEREST MAP = DANGER MAP && CONTEX VECTOR * -1!
    {
        int index = 0;

        switch (contextMap)
        {
            case Context.PushForward:
                UpdateInterestMap();
                index = HighestIndex(interestMap);
                //Debug.DrawRay(transform.position + Vector3.up, WorldToLocalDir(contextVectors[index]) * interestMap[index], Color.blue, .02f);
                desiredDir = (contextVectors[index] * interestMap[index]).sqrMagnitude >= Mathf.Pow(minMovementMagnitude, 2) ?
                             contextVectors[index] : Vector3.zero;
                break;
            //case Context.PullBack:
            //    index = HighestIndex(dangerMap);
            //    //Debug.DrawRay(transform.position + Vector3.up, WorldToLocalDir( - contextVectors[index]) * dangerMap[index], Color.blue, .02f);
            //    desiredDir = ( - contextVectors[index] * dangerMap[index]).sqrMagnitude >= Mathf.Pow(minMovementMagnitude, 2) ?
            //                 - contextVectors[index] : Vector3.zero;
            //    break;
            //case Context.Sidestep:
            //    index = HighestIndex(dangerMap);
            //    //Debug.DrawRay(transform.position + Vector3.up, WorldToLocalDir(-contextVectors[index]) * dangerMap[index], Color.blue, .02f);
            //    desiredDir = (-contextVectors[index] * dangerMap[index]).sqrMagnitude >= Mathf.Pow(minMovementMagnitude, 2) ?
            //                 -contextVectors[index] : Vector3.zero;
            //    break;
            default:
                index = HighestIndex(dangerMap);
                //Debug.DrawRay(transform.position + Vector3.up, WorldToLocalDir(-contextVectors[index]) * dangerMap[index], Color.blue, .02f);
                desiredDir = (-contextVectors[index] * dangerMap[index]).sqrMagnitude >= Mathf.Pow(minMovementMagnitude, 2) ?
                             -contextVectors[index] : Vector3.zero;
                break;
        }             

        //float interest = 0f;
        //int index = 0;
        //for (int i = 0; i < dangerMap.Length; i++)
        //{
        //    //Update Interest Map (weighed)
        //    interestMap[i] = Mathf.Clamp01(1 - dangerMap[i]) * interestMapWeights[i];

        //    //And check for the highest interest
        //    if (interestMap[i] > interest)
        //    {
        //        interest = interestMap[i];
        //        index = i;
        //    }
        //}
        //Debug.DrawRay(transform.position + Vector3.up, WorldToLocalDir(contextVectors[index]) * interestMap[index], Color.blue, .02f);
        //desiredDir = (contextVectors[index] * interestMap[index]).sqrMagnitude >= Mathf.Pow(minMovementMagnitude, 2) ?
        //             contextVectors[index] : Vector3.zero;
    }

    void UpdateInterestMap()
    {
        for (int i = 0; i < dangerMap.Length; i++)
        {
            interestMap[i] = Mathf.Clamp01(1 - dangerMap[i]) * interestMapWeights[i]; //lamba expression for delegate?
        }
    }

    int HighestIndex(float[] map) //Cin lamba experession + linq?
    {
        float interest = 0f;
        int index = 0;

        for (int i = 0; i < map.Length; i++)
        {
            if (map[i] > interest)
            {
                interest = interestMap[i];
                index = i;
            }
        }

        return index;
    }

    void UpdateAnimatorLocomotionParameters()
    {
        if (desiredDir != Vector3.zero) //Acceleration
        {
            animator.SetFloat(movingForwardHash, desiredDir.z, .1f, Time.fixedDeltaTime);
            animator.SetFloat(movingRightHash, desiredDir.x, .1f, Time.fixedDeltaTime);
        }
        else //Deceleration
        {
            animator.SetFloat(movingForwardHash, 0, .1f, Time.fixedDeltaTime); //TODO scegliere il damp time da inspector. Valutare se mettere tutto su agentAI
            animator.SetFloat(movingRightHash, 0, .1f, Time.fixedDeltaTime);
        }
    }

    Vector3 WorldToLocalDir(Vector3 direction)
    {
        return direction.z * transform.forward + direction.x * transform.right + direction.y * transform.up;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position + Vector3.up, 2f);
    }
}
