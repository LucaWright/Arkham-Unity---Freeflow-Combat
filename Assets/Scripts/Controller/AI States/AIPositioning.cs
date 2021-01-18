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

        idleHash = agentAI.idleHash;
        movingForwardHash = agentAI.movingForwardHash;
        movingBackwardHash = agentAI.movingBackwardHash;
        movingRightHash = agentAI.movingRightHash;
        movingLeftHash = agentAI.movingLeftHash;

        distanceInfo = CombatDirector.DistanceInfo;

        maxSidestep = Mathf.Max(1, maxSidestep);
    }

    public override void OnEnter()
    {
        base.OnEnter();
        agentAI.state = AgentState.Positioning;
        sidestepCount = 0;
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
        //TODO (test):
        switch (CombatDirector.state)
        {
            case CombatDirectorState.Planning:
                CheckIfCandidate();
                break;
            case CombatDirectorState.Dispatching:
                if (agentAI.currentLine == 1)
                {
                    if (CombatDirector.strikers.Contains(agentAI)) return;
                    agentAI.StopAllCoroutines();
                    StartCoroutine(agentAI.PullBack(2));
                }
                break;
            case CombatDirectorState.Executing:
                break;
            default:
                break;
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        animator.ResetTrigger(idleHash);
        animator.ResetTrigger(movingForwardHash);
        animator.ResetTrigger(movingBackwardHash); //TODO TEST: Non resettare?
        animator.ResetTrigger(movingRightHash);
        animator.ResetTrigger(movingLeftHash);
        StopAllCoroutines();
    }

    //TODO:
    //Funzione emergenza OnCollisionEnter

    IEnumerator PositioningCheckIn()
    {
        //Wait until the new movement animation starts
        while (!animator.IsInTransition(0))
        {
            yield return new WaitForFixedUpdate();
        }
        //Then, get the movement animation lenght. Plus animation average speed (we need it for movement casting later)
        clipLength = animator.GetNextAnimatorClipInfo(0)[0].clip.length;
        averageSpeed = animator.GetNextAnimatorClipInfo(0)[0].clip.averageSpeed;
        //The movement animations have exit times. So, we check surroungings each clipLength seconds. But first...
        //we must wait until the transition ends. Because we want to do this check in the middle of the animation.
        float waitTime = agentAI.animator.GetAnimatorTransitionInfo(0).duration;
        yield return new WaitForSeconds(waitTime);
        //Once positioning state is setted with new movement animation, we can do regular checks until state exit.
        StartCoroutine(EvaluatePositioning());
    }

    IEnumerator EvaluatePositioning()
    {
        CheckAround(); //Controlla se è arrivato a destinazione, se ha fatto il numero di passi necessari o se ha incontrato un ostacolo
        yield return new WaitForSeconds(clipLength);
        StartCoroutine(EvaluatePositioning());
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
        if (Physics.SphereCast(raycastOrigin, agentNM.radius, direction, out hitInfo, distanceInfo.LastLineRadius, lineOfSightLM)) //lasciare spherecast sulla chest?
        {
            return hitInfo;
        }
        return null;
    }    

    void CheckAround()
    {
        if (agentAI.movementDir == AgentMovementDir.Forward)
        {
            if (agentAI.currentLine <= agentAI.destinationLine)
            {
                StartCoroutine(agentAI.BackToIdle());
            }
            else
            if (MovementPathObstacleCheck())
            {
                //TODO
                //Valuta se fare direttamente step back. Le funzione di valutazione dovrebbe essere identica ad idle
                Debug.LogWarning("To do: valutare se fare step back diretto o andare in idle in base a distanza con oggetto hittato.");
                //Se distanza manipolata = 0, vai in step back. Altrimenti, vai in idle
            }
        }
        else      
        //if (agentAI.movementDir == AgentMovementDir.Right || agentAI.movementDir == AgentMovementDir.Left)
        {
            sidestepCount++;
            if (sidestepCount >= maxSidestep)
            {
                StartCoroutine(agentAI.BackToIdle());
            }
        }
    }

    //Finds obstacles along movement vector
    public bool MovementPathObstacleCheck()
    {
        Vector3 raycastOrigin = transform.position + Vector3.up;
        RaycastHit hitInfo;
        return Physics.SphereCast(raycastOrigin, agentNM.radius, TransformToLocal(averageSpeed), out hitInfo, averageSpeed.magnitude * clipLength, lineOfSightLM);
        //Ho bisogno anche che mi ritorni la distanza con l'oggetto, se voglioche capisca che fare.
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
        Gizmos.DrawWireSphere(raycastOrigin + TransformToLocal(averageSpeed) * clipLength, agentNM.radius);
        //Debug.Log(Vector3.Angle(transform.forward, transform.InverseTransformDirection(averageSpeed))); //cazzo c'è che non va???
    }
}
