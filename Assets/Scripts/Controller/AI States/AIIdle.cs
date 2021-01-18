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
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        StartCoroutine(IdleCheckOut());
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        if (!agentAI.IsValidLine()) return;

        //Controllo Evaluate davanti a sé sempre? Poi esegue a seconda del mio comando?
        //On Collision Enter qui?
        agentAI.HandleRootMotionMovement();
        agentAI.HandleRootMotionRotation();
        //TODO (test):
        switch (CombatDirector.state)
        {
            case CombatDirectorState.Planning:
                break;
            case CombatDirectorState.Dispatching:
                if (agentAI.currentLine == 1)
                {
                    if (CombatDirector.strikers.Contains(agentAI)) return;
                    StartCoroutine(agentAI.PullBack(2));
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
        base.OnExit();
        animator.ResetTrigger(idleHash);
        animator.ResetTrigger(movingForwardHash);
        animator.ResetTrigger(movingBackwardHash); //TODO TEST: NON RESETTARE?
        animator.ResetTrigger(movingRightHash);
        animator.ResetTrigger(movingLeftHash);
        StopAllCoroutines();
    }

    IEnumerator IdleCheckOut()
    {
        yield return new WaitForSeconds(positioningCheckTime);
        animator.ResetTrigger(idleHash);
        EvaluateMovement();
    }

    public void EvaluateMovement()
    {
        RaycastHit? hit = CheckTowardsDir(transform.forward);
        if (hit.HasValue)
        {
            var hitInfo = (RaycastHit)hit;
            PlanMovement(hitInfo);
        }
        else
        {
            Debug.LogWarning("Se sono entrato qui dentro e si è verificato un bug, è probabile sia perché il thug era troppo vicino al player e lo spherecast non lo ha rilevato.");
            //In teoria, quanto sopra NON dovrebbe succedere grazie a IsValidLine();
            //TODO: C'è entrato. Fare controllo di compenetrazione
            StartCoroutine(agentAI.PushForward(1));
        }
    }

    public RaycastHit? CheckTowardsDir(Vector3 direction)
    {
        Vector3 raycastOrigin = transform.position + Vector3.up;
        RaycastHit hitInfo;
        if (Physics.SphereCast(raycastOrigin, agentNM.radius, direction, out hitInfo, distanceInfo.LastLineRadius, lineOfSightLM))
        {
            return hitInfo;
        }
        return null;
    }

    public void PlanMovement(RaycastHit hitInfo)
    {
        switch (hitInfo.transform.tag)
        {
            case "Player":
                PushForward();
                return;
            case "Foe":
                EvaluateSidestep(hitInfo);
                break;
            default: //Avoidance Ostacoli. Dovrebbe entrare in uno stato a parte o sfruttare sempre dispatching?
                Debug.LogWarning("Manca ancora la funzione di avoidance degli ostacoli non-Character");
                //agentAI.RunForward(CombatDirector.DistanceInfo.LineDistance(agentAI.currentLine));
                break;
        }
    }

    void PushForward()
    {
        if (agentAI.currentLine == 1) return;
        StartCoroutine(agentAI.PushForward(1));
    }

    public void EvaluateSidestep(RaycastHit other)
    {
        var distance = Vector3.Distance(transform.position, other.transform.position);
        var lines = Mathf.RoundToInt(distance / distanceInfo.LineToLineDistance);
        //Se a stabilire canBackward fosse il collision enter sarebbe meglio.
        switch (lines)
        {
            case 0:
                CheckSurroundings(other.normal, true);
                break;
            case 1:
                AgentAI otherAgnetAI = other.transform.GetComponent<AgentAI>();
                AgentMovementDir otherMovementDir = otherAgnetAI.movementDir;
                if (otherMovementDir == AgentMovementDir.Backward)
                {
                    CheckSurroundings(other.normal, true);
                }
                else
                {
                    CheckSurroundings(other.normal, false);
                }
                break;
            default:
                StartCoroutine(agentAI.PushForward(agentAI.currentLine - (lines - 1)));
                break;
        }
    }


    //TODO: REFACTORING DI QUESTA FUNZIONE!
    public void CheckSurroundings(Vector3 normal, bool canBackward)
    {
        avoidanceCollider.enabled = false; //Lo spherecast prende anche sé stesso, sennò!
        RaycastHit[] hits = Physics.SphereCastAll(transform.position + Vector3.up, agentNM.radius * 3.5f, Vector3.forward, 0f, posEvaluationLM); //C'è da scegliere il raggio
        avoidanceCollider.enabled = true;

        float normalAngle = Vector3.SignedAngle(transform.forward, normal, Vector3.up);
        float angleSign = Mathf.Sign(normalAngle);

        bool canGoRight = true;
        bool canGoLeft = true;

        foreach (RaycastHit hit in hits)
        {
            if (canGoRight || canGoLeft || canBackward)
            {
                Vector3 evaluatedDir = hit.transform.position - transform.position;
                float evaluatedAngle = Vector3.SignedAngle(angleSign * transform.right, evaluatedDir, Vector3.up); //Non si può fare usando la forward? è giusto un pelo più semplice.
                //Con la trigonometria?
                if (Mathf.Abs(evaluatedAngle) < referenceAngle / 2f)
                {
                    canGoRight = false;
                }
                else
                if (Mathf.Abs(evaluatedAngle) > straightAngle - (referenceAngle / 2f))
                {
                    canGoLeft = false;
                }
                else
                if (angleSign * referenceAngle / 2f <= evaluatedAngle && evaluatedAngle <= angleSign * straightAngle - (referenceAngle / 2f))
                {
                    canBackward = false;
                }
            }
            else
            {
                StartCoroutine(IdleCheckOut());
                return;
            }
        }

        if (angleSign > 0)
        {
            
            fsm.State = agentAI.positioningState;
            if (canGoRight)
            {
                StartCoroutine(agentAI.StepRight());
                return;
            }
            else
            if (canGoLeft)
            {
                StartCoroutine(agentAI.StepLeft());
                return;
            }            
        }
        else
        {
            fsm.State = agentAI.positioningState;
            if (canGoLeft)
            {
                StartCoroutine(agentAI.StepLeft());
                return;
            }
            else
            if (canGoRight)
            {
                StartCoroutine(agentAI.StepRight());
                return;
            }            
        }
        if (canBackward) //Vedere se effettivamente utile o rilegarla nel collision enter
        {
            StartCoroutine(agentAI.PullBack(agentAI.currentLine + 1));
        }
    }

    private void OnDrawGizmos()
    {
        if (agentAI != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position + Vector3.up, distanceInfo.LineToLineDistance);
        }
    }
}
