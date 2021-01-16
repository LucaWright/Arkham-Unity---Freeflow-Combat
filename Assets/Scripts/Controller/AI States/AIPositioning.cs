using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEditor;
using UnityEngine;

public class AIPositioning : State
{
    public AIPositioning(GameObject go, StateMachine fsm) : base(go, fsm) { }

    AgentAI agentAI;

    float clipLength = 0;
    Vector3 averageSpeed;

    public int maxSidestep = 2;
    int sidestepCount = 0;

    //timer?
    //stringtohash?

    private void Awake()
    {
        agentAI = GetComponent<AgentAI>();
    }

    void Start()
    {        
        go = this.gameObject;
        fsm = agentAI.fsm;
        maxSidestep = Mathf.Max(1, maxSidestep); //non può essere minore di 1.
    }

    public override void OnEnter()
    {
        base.OnEnter();
        agentAI.state = AgentState.Positioning;
        sidestepCount = 0; //Alternativa al count
        StartCoroutine(PositioningCheckIn());
    }

    public override void OnUpdate()
    {
        base.OnUpdate();  
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        if (!agentAI.IsValidLine()) return;
        agentAI.HandleRootMotionMovement();
        agentAI.HandleRootMotionRotation();
        if (CombatDirector.state == CombatDirectorState.Planning)
        {
            CheckIfCandidate();
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        StopAllCoroutines();
    }

    void CheckIfCandidate()
    {
        RaycastHit? hit = CheckTowardsDir(transform.forward);
        if (hit.HasValue)
        {
            var hitInfo = (RaycastHit)hit;
            
            if (hitInfo.transform.tag == "Player")
            {
                CombatDirector.AddToStrikersCandidateList(agentAI, agentAI.currentLine);
            }
        }
    }

    public RaycastHit? CheckTowardsDir(Vector3 direction) //RIPETUTA! DRY!
    {
        Vector3 raycastOrigin = transform.position + Vector3.up;
        RaycastHit hitInfo;
        if (Physics.SphereCast(raycastOrigin, agentAI.agentNM.radius, direction, out hitInfo, CombatDirector.DistanceInfo.LastLineRadius, agentAI.agentLM)) //lasciare spherecast sulla chest?
        {
            return hitInfo;
        }
        return null;
    }

    IEnumerator PositioningCheckIn()
    {
        //Wait until the new movement animation starts
        while (!agentAI.animator.IsInTransition(0))
        {
            yield return new WaitForFixedUpdate();
        }
        //Then, get the movement animation lenght. Plus animation average speed (we need it for movement casting later)
        clipLength = agentAI.animator.GetNextAnimatorClipInfo(0)[0].clip.length;
        averageSpeed = agentAI.animator.GetNextAnimatorClipInfo(0)[0].clip.averageSpeed;
        //The movement animations have exit times. So, we check surroungings each clipLength seconds. But first...
        //we must wait until the transition ends. Because we want to do this check in the middle of the animation.
        yield return new WaitForSeconds(agentAI.animator.GetAnimatorTransitionInfo(0).duration);
        //Once positioning state is setted with new movement animation, we can do regular checks until state exit.
        StartCoroutine(EvaluatePositioning());
    }

    IEnumerator EvaluatePositioning()
    {
        CheckAround(); //Controlla se è arrivato a destinazione, se ha fatto il numero di passi necessari o se ha incontrato un ostacolo
        yield return new WaitForSeconds(clipLength);
        StartCoroutine(EvaluatePositioning());
    }

    IEnumerator PositioningCheckOut()
    {
        while (!agentAI.animator.IsInTransition(0))
        {
            yield return new WaitForFixedUpdate();
        }
        yield return new WaitForSeconds(agentAI.animator.GetAnimatorTransitionInfo(0).duration);
        yield return new WaitForFixedUpdate();
        fsm.State = agentAI.idleState;
        //Debug.Break();
    }

    void CheckAround()
    {
        if (agentAI.currentLine == agentAI.destinationLine || MovementPathObstacleCheck()) //Se trova obstacle può fare cose diverse!
        {
            //agentAI.BackToIdle();
            //StartCoroutine(PositioningCheckOut());
            StartCoroutine(agentAI.BackToIdle1());
        }

        if (agentAI.movementDir == AgentMovementDir.Right || agentAI.movementDir == AgentMovementDir.Left)
        {
            sidestepCount++;
            if (sidestepCount >= maxSidestep)
            {
                //agentAI.BackToIdle();
                //StartCoroutine(PositioningCheckOut());
                StartCoroutine(agentAI.BackToIdle1());
            }
        }
    }

    //Finds obstacles along movement vector
    public bool MovementPathObstacleCheck()
    {
        Vector3 raycastOrigin = transform.position + Vector3.up;
        RaycastHit hitInfo;
        return Physics.SphereCast(raycastOrigin, agentAI.agentNM.radius, TransformToLocal(averageSpeed), out hitInfo, averageSpeed.magnitude * clipLength, agentAI.agentLM);
        //Per ora, si limita a fermarsi. Le direzioni sono avanti, destra, sinistra. La posizione indietro (che obbliga all'oggetto hittato di scansarsi) sta nello stato di retreat.
        //Lo stato di retreat può essere inglobato qui?
    }

    public Vector3 TransformToLocal(Vector3 reference) //in alternativa a InverseTransformDirection, che non capisco perché stia dando errore
    {
        return transform.forward * reference.z + transform.right * reference.x + transform.up * reference.y;
    }

    

    

    private void OnDrawGizmos()
    {
        //Debug.Log(averageSpeed);
        if (agentAI == null) return;

        Vector3 raycastOrigin = transform.position + Vector3.up;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(raycastOrigin, TransformToLocal(averageSpeed) * clipLength);
        //Gizmos.DrawRay(raycastOrigin, (averageSpeed) * clipLength);
        Gizmos.DrawWireSphere(raycastOrigin + TransformToLocal(averageSpeed) * clipLength, agentAI.agentNM.radius);
        //Gizmos.DrawWireSphere(raycastOrigin + (averageSpeed) * clipLength, agentAI.agentNM.radius);

        //Gizmos.color = Color.blue;
        //Debug.DrawRay(raycastOrigin, transform.forward * averageSpeed.z + transform.right * averageSpeed.x);

        //Debug.Log(Vector3.Angle(transform.forward, transform.InverseTransformDirection(averageSpeed))); //cazzo c'è che non va???
    }
}
