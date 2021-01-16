using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class AIIdle : State
{
    public AIIdle(GameObject go, StateMachine fsm) : base(go, fsm) { }

    AgentAI agentAI;

    float timer = 0;
    public float positioningCheckTime = 1.5f; //Valutare se trasformarlo in Vector2 e scegliere randomicamente
    [HideInInspector] public int priority = 0;

    public LayerMask positioningLayerMask;

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

    public override void OnFixedUpdate() //COME FUNZIONANO LE FUNZIONI PUSH TO??
    {
        base.OnFixedUpdate();
        if (!agentAI.IsValidLine()) return; //Serve in LOCAL, in quando può chiamare altri stati

        agentAI.HandleRootMotionMovement();
        agentAI.HandleRootMotionRotation();
    }

    public override void OnExit()
    {
        base.OnExit();
        StopAllCoroutines();
    }

    IEnumerator IdleCheckOut()
    {
        yield return new WaitForSeconds(positioningCheckTime);
        for (int i = 0; i < priority; i++) //non può essere inferiore a 1?
        {
            yield return new WaitForFixedUpdate();
        }
        
        agentAI.animator.ResetTrigger(agentAI.idleHash); //di sicurezza, dato che può entrare qui da idle e da positioning
        EvaluateMovement();
    }

    public void EvaluateMovement() //Movement, più che position
    {
        RaycastHit? hit = CheckTowardsDir(transform.forward); //Posso riportarla in LOCAL
        if (hit.HasValue)
        {
            var hitInfo = (RaycastHit)hit;
            PlanNextMovementAction(hitInfo);
        }
        else
        {
            //Se non hitta nulla?
            //In teoria, si deve riposizionare.
            //Sempre più convinto che serva uno stato specifico per questa situazione

            //Candidate = false;
            //agentAI.PushForward(1);
            //fsm.State = agentAI.positioningState;
            StartCoroutine(agentAI.PushForward1(1));
        }
    }

    public RaycastHit? CheckTowardsDir(Vector3 direction) //CheckToward? Dovrebbe valere a prescindere dalla direzione
    {
        Vector3 raycastOrigin = transform.position + Vector3.up;
        RaycastHit hitInfo;
        if (Physics.SphereCast(raycastOrigin, agentAI.agentNM.radius, direction, out hitInfo, CombatDirector.DistanceInfo.LastLineRadius, agentAI.agentLM)) //lasciare spherecast sulla chest?
        {
            return hitInfo;
        }
        return null;
    }

    public void PlanNextMovementAction(RaycastHit hitInfo)
    {
        switch (hitInfo.transform.tag)
        {
            //Hits Characters
            case "Player":
                MoveForward(); //Vedi se basta un push forward o serve una coroutine.
                return;
            case "Foe":               
                EvaluateMovement(hitInfo); //Questa funzione la voglio fare un po' diversa... Al momento, la teniamo così
                break;
            //Hit Obstacle. Avoid obstacle
            default: //Crea function con nome fico
                Debug.Log("Valuto");
                //agentAI.RunForward(CombatDirector.DistanceInfo.LineDistance(agentAI.currentLine)); //Forse, vale la pena fare uno stato apposta.
                break;
        }
    }

    void MoveForward()
    {
        //CombatDirector.AddToStrikersCandidateList(agentAI, agentAI.currentLine);
        if (agentAI.currentLine == 1) return;
        //agentAI.PushForward(1);
        //fsm.State = agentAI.positioningState;
        StartCoroutine(agentAI.PushForward1(1));
    }

    public void EvaluateMovement(RaycastHit other)
    {
        agentAI.isCandidate = false;

        var distance = Vector3.Distance(transform.position, other.transform.position);
        var lines = Mathf.RoundToInt(distance / CombatDirector.DistanceInfo.LineToLineDistance);
        switch (lines)
        {
            case 0:
                //Considerando che è l'altro Thug a obbligarlo a retrocedere, potrebbe: DIRECT RETREAT
                CheckSurroundings(other.normal, true);
                break;
            case 1: //DEVE CAPIRE SOLO SE ANDARE A DESTRA O A SINISTRA
                //Se potessi scegliere l'ordine in cui vengono controllati i Thugs, sarebbe meno problematico
                AgentAI otherAgnetAI = other.transform.root.GetComponent<AgentAI>();
                AgentMovementDir otherMovementDir = otherAgnetAI.movementDir;
                if (otherMovementDir == AgentMovementDir.Backward)
                {
                    CheckSurroundings(other.normal, true); //e se fossero gli altri, muovendosi, a mandare i segnali???
                }
                else
                {
                    CheckSurroundings(other.normal, false);
                }
                break;
            default:
                //Questo è facile
                //agentAI.PushForward(agentAI.currentLine - (lines - 1));
                //fsm.State = agentAI.positioningState;
                StartCoroutine(agentAI.PushForward1(agentAI.currentLine - (lines - 1)));
                break;
        }
    }

    public void CheckSurroundings(Vector3 normal, bool canBackward) //Questa funzione è una merda. Va rivista
    {
        agentAI.avoidanceCollider.enabled = false; //Lo spherecast prende anche sé stesso, sennò!
        RaycastHit[] hits = Physics.SphereCastAll(transform.position + Vector3.up, agentAI.agentNM.radius * 3.5f, Vector3.forward, 0f, agentAI.agentLM); //C'è da scegliere il raggio
        agentAI.avoidanceCollider.enabled = true;

        float normalAngle = Vector3.SignedAngle(transform.forward, normal, Vector3.up);
        float angleSign = Mathf.Sign(normalAngle);

        bool canGoRight = true;
        bool canGoLeft = true;

        foreach (RaycastHit hit in hits)
        {
            if (canGoRight || canGoLeft || canBackward)
            {
                Vector3 evaluatedDir = hit.transform.position - transform.position;
                float evaluatedAngle = Vector3.SignedAngle(angleSign * transform.right, evaluatedDir, Vector3.up); //Senso orario = positivo

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
                //agentAI.BackToIdle();
                StartCoroutine(IdleCheckOut());
                return;
            }
        }

        if (angleSign > 0)
        {
            
            fsm.State = agentAI.positioningState;
            if (canGoRight)
            {
                //agentAI.StepRight();
                StartCoroutine(agentAI.StepRight1());
                return;
            }
            else
            if (canGoLeft)
            {
                //agentAI.StepLeft();
                StartCoroutine(agentAI.StepLeft1());
                return;
            }            
        }
        else
        {
            fsm.State = agentAI.positioningState;
            if (canGoLeft)
            {
                agentAI.StepLeft();
                StartCoroutine(agentAI.StepLeft1());
                return;
            }
            else
            if (canGoRight)
            {
                //agentAI.StepRight();
                StartCoroutine(agentAI.StepRight1());
                return;
            }            
        }
        if (canBackward)
        {
            //agentAI.PullBack(agentAI.currentLine + 1); //meglio metterla come parametro del metodo? In questo modo, si è sicuri di non dimenticare mai di settare la destinazione
            //fsm.State = agentAI.retreatState;
            StartCoroutine(agentAI.PullBack1(agentAI.currentLine + 1));
        }
    }

    private void OnDrawGizmos()
    {
        if (agentAI != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position + Vector3.up, CombatDirector.DistanceInfo.LineToLineDistance);
        }
    }
}
