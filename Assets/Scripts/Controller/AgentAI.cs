using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

public enum AgentState { Idle, Positioning, Dispatching, Attacking, Retreat, Recover }

public class AgentAI : MonoBehaviour
{
    //public CombatDirector director;
    
    NavMeshAgent agentNM;
    Animator anim;
    Rigidbody rb;

    public AgentState state = AgentState.Idle;

    public Transform target;

    public GameObject CounterUI;

    public LayerMask layer;

    public int oldLine = -1;
    public int currentLine = 4;
    public int nextLine = 1;

    public bool isEvaluatingPosition = false;
    public bool mustRun = false;

    public bool canAttack = false;
    public bool isWaitingAttackSignal = false;

    public float positioningMaxTime = 3;
    float positioningTime = 0;

    //vedere le string to hash.
    public bool isCandidate;

    RaycastHit hitInfo;

    Vector3 rootMotion;

    public Transform rootBone;
    public List<Rigidbody> bodyParts;
    int aiBodyPartLayer = -1;


    public int comboCounter = 0;
    bool wantAttack;
    public bool isAttacking;

    private void Awake()
    {      
        agentNM = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        //CounterUI = Instantiate(CounterUI, anim.GetBoneTransform(HumanBodyBones.Head));
        //SetCounterUI(false);
    }

    private void OnEnable()
    {
        
    }

    private void Start()
    {
        agentNM.updateRotation = false;
        target = CombatDirector.DistanceInfo.Target;
        currentLine = CombatDirector.DistanceInfo.GetLine(this.transform);
        CombatDirector.AddToListLine(this, oldLine, currentLine);
    }

    private void OnAnimatorMove()
    {
        rootMotion += anim.deltaPosition;
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case AgentState.Idle:
                HandleMovement();
                HandleRotation();
                UpdateOnIdle();
                //Qui quando tutto è resettato
                //Check se si può muovere in qualche direzione
                break;
            case AgentState.Positioning:
                HandleMovement();
                HandleRotation();
                //Si sta muovendo e deve verificare se è arrivato alla linea target
                break;
            case AgentState.Dispatching:
                HandleMovement();
                HandleRotation();
                //Sta correndo verso l'obiettivo
                break;
            case AgentState.Attacking:
                HandleMovement();
                HandleRotation();
                //Sono tutti in posizione e stanno attaccando
                break;
            case AgentState.Retreat:
                HandleMovement();
                HandleRotation();
                //È alla linea 0 o ha finito di attaccare
                break;
            case AgentState.Recover:
                //è a terra e deve rialzarsi
                break;
            default:
                break;
        }
    }

    void HandleMovement()
    {
        if (!agentNM.hasPath)
        {
            agentNM.Move(rootMotion);
        }
        else
        {
            agentNM.velocity = rootMotion / Time.fixedDeltaTime;
            agentNM.speed = agentNM.velocity.magnitude;
        }

        rootMotion = Vector3.zero;
    }

    void HandleRotation()
    {
        if (!agentNM.hasPath)
        {
            var towardsDirection = (target.position - transform.position); //mettere funzione di calcolo
            Quaternion lookRotation = Quaternion.LookRotation(towardsDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, .1f);
        }
        else
        {
            var towardsDirection = agentNM.desiredVelocity != Vector3.zero ? agentNM.desiredVelocity : (target.position - transform.position); //funzione calcolo
            Quaternion lookRotation = Quaternion.LookRotation(towardsDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, .1f);
        }
    }

    Vector3 DistanceToTarget()
    {
        return Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
    }

    void UpdateOnIdle()
    {
        //Se è fermo, non ha bisogno di valutare dove sta, ma deve solo fare i check di posizione
        EvaluatePosition();
    }

    public void EvaluatePosition() //return boolean per capire se può essere inserito nella Queue dei candidati o meno?
    {
        //currentLine = CombatDirector.GetLine(this.transform);

        if (currentLine == 0)
        {
            state = AgentState.Retreat;
            return;
        }

        if (currentLine > CombatDirector.DistanceInfo.Lines) //Non proprio. Questo risultato può arrivare dopo.
        {
            anim.SetBool("Run", true);
            return;
        }
        else
        {
            anim.SetBool("Run", false);
        }

        if (CheckOnDirection(transform.forward)) //probabilmente, è meglio dare anche una direction;
        {
            var other = hitInfo.collider;
            switch (other.tag)
            {
                case "Enemy":
                    isCandidate = false;

                    var distance = Vector3.Distance(transform.position, other.transform.position);
                    var lineDistance = Mathf.RoundToInt(distance / CombatDirector.DistanceInfo.LineToLineDistance);

                    if (lineDistance > 1)
                    {
                        //nextLine = currentLine - 1
                        //move forward
                        //>>>>>>>>>>>>>>>>>>Vai a locomotion e resetta timer
                        return;
                    }
                    //else... check a destra e sinistra!
                    CheckSurroundings();
                    break;
                case "Player":
                    isCandidate = true;
                    //nextLine = 1;
                    //>>>>>>>>>>>>>>>>>Vai a locomotion e resetta timer
                    break;                
                default:
                    //Ha intercettato un qualche altro tipo di ostacolo
                    //Ergo: attiva il navmesh e continua a muoversi verso il player fino a quando lo spherecast non intercetta o un enemy o il player
                    //>>>>>>>>>>>>>Vai a positioning e resetta timer
                    break;
            }
            //se entra qui... non deve guardarsi né a destra, né a sinistra;
        }
        else
        {
            CheckSurroundings();
        }

        //mettere in check sorrounding 

        

        //Vector3 origin = transform.position + Vector3.up;
        //RaycastHit hit;

        //if (Physics.SphereCast(origin, agentNM.radius, transform.forward, out hit, Mathf.Infinity, layer)) //.5f dovrebbe essere preso dal navmeshagent, ed è il raggio di avoiding
        //{
        //    var other = hit.collider;

        //    switch (other.tag)
        //    {
        //        case "Enemy":

        //            var distance = hit.distance;
        //            positioningTime += Time.fixedDeltaTime;

        //            if (positioningTime >= positioningMaxTime)
        //            {
        //                positioningTime = 0;
        //                ResetAnimationBools();
        //                director.locomotionState.Remove(this);
        //                director.idleState[this.currentLine].Add(this);
        //                return;
        //            }

        //            if (distance >= distanceHandler.GetLinesDistance())
        //            {
        //                nextLine = distanceHandler.GetLine(distance);
        //                anim.SetBool("StepForward", true);
        //            }
        //            else
        //            {
        //                var normal = hit.normal;
        //                var signedAngle = Vector3.SignedAngle(transform.forward, normal, Vector3.up);

        //                if (signedAngle >= 0)
        //                {
        //                    anim.SetBool("StepRight", true);
        //                }
        //                else
        //                {
        //                    anim.SetBool("StepLeft", true);
        //                }
        //            }

        //            break;
        //        case "Player":

        //            director.attackingCandidates[currentLine - 1].Enqueue(this);//Entra nei candidati attaccanti. Il -1 escluside la "linea zero", in cui i personaggi devono andare indietro e in recoil
        //            nextLine = 1; //Avendo campo libero, può andare fino alla prima linea.

        //            if (currentLine != nextLine)
        //            {
        //                anim.SetBool("StepForward", true);
        //            }
        //            else
        //            {
        //                ResetAnimationBools();
        //                director.locomotionState.Remove(this);
        //                director.idleState[currentLine].Add(this);
        //                state = AgentState.Idle;
        //            }
        //            break;
        //        default: //potrebbe intercettare ostacoli, e li dovrebbe correre fino a quando non torna ad avere davanti un avversario o il giocatore.
        //            director.locomotionState.Remove(this);
        //            director.idleState[currentLine].Add(this);
        //            break;
        //    }
        //}
    }

    void CheckSurroundings()
    {
        if (CheckOnDirection(transform.right)) //non è forward, ma forward + angolo prossimo a 90°
        {
            if (CheckOnDirection(-transform.right)) //non è forward, ma forward + angolo opposoto (circa -90°)
            {
                state = AgentState.Idle; //è circondato... non si muove;
            }
            else
            {
                //step left
            }
        }
        else
        {
            //step right
        }
    }

    void PositioningController()
    {
        currentLine = CombatDirector.DistanceInfo.GetLine(transform);
        if (currentLine == nextLine)
        {
            //DoStuff;
            state = AgentState.Idle;
        }
    }

    public bool CheckOnDirection(Vector3 direction)
    {
        return Physics.SphereCast(anim.GetBoneTransform(HumanBodyBones.Chest).position, agentNM.radius, direction, out hitInfo, CombatDirector.DistanceInfo.LastLineRadius, layer);
    }

    public void Dispatch()
    {
        ResetAnimationBools();
        anim.SetTrigger("RunTowardsTarget");
        state = AgentState.Dispatching;
    }

    public void OnAttacking() //trasformarla in bool?
    {
        if (canAttack) return;

        agentNM.SetDestination(target.position);

        if (!agentNM.hasPath) return; //magari con !isPathPending. Solo dopo si può usare remainingDistance.

        Debug.DrawRay(transform.position + Vector3.up, agentNM.desiredVelocity * 5, Color.green);

        for (int i = 0; i < agentNM.path.corners.Length - 1; i++)
        {
            Debug.DrawLine(agentNM.path.corners[i], agentNM.path.corners[i + 1], Color.cyan, 0.02f);
        }

        if (DistanceToTarget().magnitude > agentNM.stoppingDistance) return;

        anim.SetTrigger("InPlace");
        ResetAnimationBools();

        canAttack = true;
    }

    public void ResetAnimationBools()
    {
        anim.SetBool("StepForward", false);
        anim.SetBool("StepLeft", false);
        anim.SetBool("StepRight", false);
        anim.SetBool("StepBack", false);
    }

    public bool CanAttack()
    {
        return canAttack;
    }

    public void Attack()
    {
        anim.ResetTrigger("InPlace");
        anim.ResetTrigger("RunTowardsTarget");
        anim.SetTrigger("Attack");
        ResetAnimationBools();
        canAttack = false;
        //SetCounterUI(true);
    }

    //public void Recoil()
    //{
    //    //agentNM.ResetPath();
    //    //currentLine = distanceHandler.GetLine(DistanceToTarget().magnitude);

    //    //if (currentLine < 1)
    //    //{
    //    //    anim.SetBool("StepBack", true);
    //    //}
    //    //else
    //    //{
    //    //    ResetAnimationBools();
    //    //    director.recoilState.Remove(this);
    //    //    director.idleState[currentLine].Add(this);
    //    //}
    //}

    //public void SetCounterUI(bool inputBool)
    //{
    //    CounterUI.SetActive(inputBool);
    //}

    //public void GetStunned(Vector3 force) //DISATTIVARE COLLIDER!
    //{
    //    anim.SetTrigger("Stunned");
    //    rb.isKinematic = false; //il rigidbody, pur restando attivo, non dà problemi. Questo potrebbe permettere anche di far sì che i nemici si scontrino a vicenda?
    //    rb.AddForce(force, ForceMode.Impulse);

    //    //QUANDO UNO VA IN GET STUNNED VA ELIMINATO DA QUALSIASI LISTA IN CUI SI TROVA!!!
    //    //Per questo: TENIAMO TRACCIA DEGLI STATE PER OGNI SINGOLO FOE, COSì DA SAPERE IN QUALE LISTA RIMUOVERLI!

    //    //Si disattiva comunque il rigidbody?
    //    //Si potrebbe fare quando va in sleep, innescando l'animazione di recupero.

    //    //Va in RECOVER, non RECOIL
    //    state = AgentState.Recover;
    //    SetCounterUI(false);

    //    agentNM.ResetPath();
    //    //agent.isStopped = false;

    //    ResetAnimationBools();

    //    //director.FindListState(this).Remove(this);
    //    //director.recoverState.Add(this);
    //}

    //public void GetCounterStun(Vector3 force) //DISATTIVARE COLLIDER!
    //{
    //    anim.SetTrigger("CounterStun");
    //    rb.isKinematic = false;
    //    rb.AddForce(force, ForceMode.Impulse);

    //    state = AgentState.Recover;
    //    SetCounterUI(false);

    //    agentNM.ResetPath();
    //    //agent.isStopped = false;
    //    ResetAnimationBools();

    //    //director.FindListState(this).Remove(this);
    //    //director.recoverState.Add(this);
    //}

    //public void OnAttackEnd() //ANIMATION EVENT
    //{
    //    //director.strikers.Remove(this);
    //    //director.recoilState.Add(this);
    //    SetCounterUI(false);

    //    if (Physics.Raycast(transform.position + Vector3.up, transform.forward, agentNM.stoppingDistance * 1.25f)) //il bersaglio deve essere in range!
    //    {
    //        target.GetComponent<Combat>().GetStunned();
    //    }
    //}

    //public void ReturnToIdleState() //ANIMATION EVENT
    //{
    //    state = AgentState.Idle;
    //    director.recoverState.Remove(this);
    //    director.idleState[GetCurrentLine()].Add(this);
    //    rb.isKinematic = true;

    //    //director.IdleListDebugger();
    //}

    //public void StopLocomotion()
    //{
    //    //agent.isStopped = true;
    //    anim.SetTrigger("Stop");
    //    agentNM.ResetPath();
    //    state = AgentState.Idle;
    //    director.FindListState(this).Remove(this);
    //    director.recoilState.Add(this);
    //}



}
