using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class AIPositioning : State
{
    public AIPositioning(GameObject go, StateMachine fsm) : base(go, fsm) { }

    AgentAI agentAI;
    Animator animator;
    NavMeshAgent agentNM;

    bool checkInEnded;

    int idleHash;
    int movingForwardHash;
    int movingBackwardHash;
    int movingRightHash;
    int movingLeftHash;

    float clipLength = 0;
    Vector3 averageSpeed;

    public int maxSidestep = 2;
    int sidestepCount = 0;
    DistanceHandler distanceInfo;
    public LayerMask lineOfSightLM;


    //private void Awake()
    //{
    //    agentAI = GetComponent<AgentAI>();
    //}

    //void Start()
    //{        
    //    go = this.gameObject;
    //    fsm = agentAI.fsm;
    //    animator = agentAI.animator;
    //    agentNM = agentAI.agentNM;

    //    idleHash = agentAI.idleHash;
    //    movingForwardHash = agentAI.movingForwardHash;
    //    movingBackwardHash = agentAI.movingBackwardHash;
    //    movingRightHash = agentAI.movingRightHash;
    //    movingLeftHash = agentAI.movingLeftHash;

    //    distanceInfo = CombatDirector.DistanceInfo;

    //    maxSidestep = Mathf.Max(1, maxSidestep); //obsoleto?
    //}

    //public override void OnEnter()
    //{
    //    base.OnEnter();
    //    agentAI.state = AgentState.Positioning;
    //    sidestepCount = 0;
    //    checkInEnded = false;
    //    StartCoroutine(PositioningCheckIn());
    //}    

    //public override void OnUpdate()
    //{
    //    base.OnUpdate();  
    //}

    //public override void OnFixedUpdate()
    //{
    //    base.OnFixedUpdate();
    //    agentAI.HandleRootMotionMovement();
    //    agentAI.HandleRootMotionRotation();
    //    if (!checkInEnded) return;
    //    if (!agentAI.IsValidLine()) //Viene chiamato in continuazione finché non c'è cambio stato!!
    //    {
    //        StopAllCoroutines();
    //        //lo mando in un fake idle?
    //        return; //Serve ancora? Davvero? Dato che Player è Character, conta anche lui nel calcolo dei soorundings. Anche sé... Quella funzione non verrebbe mai chiamata, se c'è via libera verso il player. Meglio tenerla.
    //    }
    //    //TODO (test):
    //    switch (CombatDirector.state)
    //    {
    //        case CombatDirectorState.Planning:
    //            agentAI.HasGreenLightToTheTarget(); //Serve davvero?
    //            break;
    //        case CombatDirectorState.Dispatching:
    //            if (agentAI.currentLine == 1)
    //            {
    //                if (CombatDirector.strikers.Contains(agentAI)) return; //TODO: idem come in Idle
    //                agentAI.StopAllCoroutines();
    //                //StartCoroutine(agentAI.PullBack(2));
    //                agentAI.PullBack(2);
    //            }
    //            break;
    //        case CombatDirectorState.Executing:
    //            break;
    //        default:
    //            break;
    //    }
    //}

    //public override void OnExit()
    //{
    //    base.OnExit();
    //    //animator.ResetTrigger(idleHash);
    //    //animator.ResetTrigger(movingForwardHash);
    //    //animator.ResetTrigger(movingBackwardHash); //TODO TEST: Non resettare?
    //    //animator.ResetTrigger(movingRightHash);
    //    //animator.ResetTrigger(movingLeftHash);
    //    StopAllCoroutines();
    //}

    ////TODO:
    ////Funzione emergenza OnCollisionEnter

    //IEnumerator PositioningCheckIn()
    //{
    //    //Wait until the new movement animation starts
    //    while (!animator.IsInTransition(0))
    //    {
    //        yield return new WaitForFixedUpdate();
    //    }
    //    //Then, get the movement animation lenght. Plus animation average speed (we need it for movement casting later)
    //    clipLength = animator.GetNextAnimatorClipInfo(0)[0].clip.length;
    //    averageSpeed = animator.GetNextAnimatorClipInfo(0)[0].clip.averageSpeed;
    //    //The movement animations have exit times. So, we check surroungings each clipLength seconds. But first...
    //    //we must wait until the transition ends. Because we want to do this check in the middle of the animation.
    //    float waitTime = agentAI.animator.GetAnimatorTransitionInfo(0).duration;
    //    yield return new WaitForSeconds(waitTime);
    //    checkInEnded = true;
    //    //Once positioning state is setted with new movement animation, we can do regular checks until state exit.
    //    StartCoroutine(EvaluateNewPositioning());
    //}

    //IEnumerator EvaluateNewPositioning()
    //{
    //    if (agentAI.HasGreenLightToTheTarget()) //Se ha via libera...
    //    {
    //        switch (agentAI.movementDir)
    //        {
    //            case AgentMovementDir.Forward:
    //                if (agentAI.currentLine == 1)
    //                {
    //                    //StartCoroutine(agentAI.BackToIdle()); //La coroutine verrà fermata dall'uscita dello stato.
    //                    agentAI.BackToIdle();
    //                }
    //                break;
    //            default:
    //                //StartCoroutine(agentAI.PushForward(1));
    //                agentAI.PushForward(1); //PushForward lo manda in Idle se è già a 1
    //                break;
    //        }
    //        //if (agentAI.movementDir != AgentMovementDir.Forward)//...e non è già in forward
    //        //{
    //        //    StartCoroutine(agentAI.PushForward(1));
    //        //    yield break;
    //        //}
    //        //else
    //        //{
    //        //    if (agentAI.currentLine == 1)
    //        //    {
    //        //        StartCoroutine(agentAI.BackToIdle());
    //        //        yield break;
    //        //    }
    //        //}
    //    }
    //    else
    //    {
    //        agentAI.CheckSurroundings();
    //    }
    //    yield return new WaitForSeconds(clipLength);
    //    StartCoroutine(EvaluateNewPositioning());
    //}

    ////public Vector3 TransformToLocal(Vector3 reference) //in alternativa a InverseTransformDirection, che non capisco perché stia dando errore
    ////{
    ////    return transform.forward * reference.z + transform.right * reference.x + transform.up * reference.y;
    ////} 

    //private void OnDrawGizmos()
    //{
    //    ////Debug.Log(averageSpeed);
    //    //if (agentAI == null) return;

    //    //Vector3 raycastOrigin = transform.position + Vector3.up;
    //    //Gizmos.color = Color.red;
    //    //Gizmos.DrawRay(raycastOrigin, TransformToLocal(averageSpeed) * clipLength);
    //    //Gizmos.DrawWireSphere(raycastOrigin + TransformToLocal(averageSpeed) * clipLength, agentNM.radius);
    //    ////Debug.Log(Vector3.Angle(transform.forward, transform.InverseTransformDirection(averageSpeed))); //cazzo c'è che non va???
    //}
}
