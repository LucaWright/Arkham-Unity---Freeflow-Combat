using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

public enum AgentState { Idle, Positioning, Dispatching, Attacking, Retreat, Recover } //Vedere se mettere uno stato di trasferimento da una linea all'altra, che controlla solo se di è giunta la destinazione. Dovrebbe essere una derivata del positioning, però.
public enum AgentMovementDir { None, Forward, Backward, Right, Left }

public class AgentAI : MonoBehaviour
{
    public AgentState state = AgentState.Idle;
    public AgentMovementDir movementDir = AgentMovementDir.None;
    
    NavMeshAgent agentNM;
    Rigidbody rb;
    Animator anim;
    //triggers
    int movingIdleHash;
    int movingForwardHash;
    int movingBackwardHash;
    int movingRightHash;
    int movingLeftHash;

    public float positioningEvaluationTime = 3;
    float positioningTimer = 0;

    public LayerMask raycastLayerMask;

    public Transform target;

    public GameObject CounterUI;


    //public int oldLine = -1;
    public int currentLine = -1;
    public int nextLine = 1;

    public bool isEvaluatingPosition = false;
    public bool mustRun = false;

    public bool canAttack = false;
    public bool isWaitingAttackSignal = false;


    //vedere le string to hash.
    public bool isCandidate;

    //RaycastHit hitInfo;

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
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        SetAnimatorStringsToHash();

        //CounterUI = Instantiate(CounterUI, anim.GetBoneTransform(HumanBodyBones.Head));
        //SetCounterUI(false);
    }

    void SetAnimatorStringsToHash()
    {
        movingIdleHash = Animator.StringToHash("Idle");
        movingForwardHash = Animator.StringToHash("Step Forward");
        movingBackwardHash = Animator.StringToHash("Step Backward");
        movingRightHash = Animator.StringToHash("Step Right");
        movingLeftHash = Animator.StringToHash("Step Left");
    }

    private void OnEnable()
    {
        
    }

    private void Start()
    {
        agentNM.updateRotation = false;
        target = CombatDirector.DistanceInfo.Target;
        CheckCurrentLine();
    }

    void CheckCurrentLine()
    {
        int oldLine = currentLine;
        currentLine = CombatDirector.DistanceInfo.GetLine(this.transform);
        if (currentLine != oldLine)
        {
            //ChangeList
            CombatDirector.ChangeLineList(this, oldLine, currentLine);
        }
    }
    
    bool IsValidLine()
    {
        currentLine = CombatDirector.DistanceInfo.GetLine(this.transform);

        if (currentLine == 0)
        {
            //Do stuff and return false
            nextLine = 1;
            state = AgentState.Retreat;
            return false;
        }
        else
        if (currentLine > CombatDirector.DistanceInfo.Lines) //Non proprio. Questo risultato può arrivare dopo.
        {
            //Do sfuff and return false
            //> Se è sulla linea LastLine + 1, deve ritornare in posizione. Per farlo, deve usare per forza il navmesh agent e fermarsi quando torna sull'ultima linea.
            anim.SetBool("Run", true);
            return false;
        }

        //Deve necessariamente tornare in Idle?
        return true;
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case AgentState.Idle:
                
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

    private void OnAnimatorMove()
    {
        rootMotion += anim.deltaPosition;
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
        if (!IsValidLine()) return;

        EvaluateMovement(); //Vedere se return type o spostare effetivamente per ultima
        HandleMovement();
        HandleRotation();
    }

    void UpdateOnPositioning() //Ci sono due modalità: raggiungi la destinazione o continua per tot tempo
    {        
        positioningTimer += Time.fixedDeltaTime;

        HandleMovement();
        HandleRotation();
        if (positioningTimer >= positioningEvaluationTime)
        {
            positioningTimer = 0;
            PushToIdle();
        }
    }

    //Planning!!!
    //CheckSorroundings
    //Per capire come muoversi, il nostro amico Thug deve guardarsi intorno con due mezzi:
    //1. SphereCast verso Target > Per vedere se ha strada libera verso il target
    //2. SphereCast attorno a sé entro un raggio, per capire com'è attorno a lui. (Vedi come si gestiscono i boids.)
    //Se da 2. si deve muovere, lo deve fare per un massimo di X secondi prima di tornare in idle.

    //Prima ancora di 1.
    //> Il Thug deve capire su che linea è
    //> Se è sulla linea 0, deve ritirarsi.
    //> Se è sulla linea LastLine + 1, deve ritornare in posizione. Per farlo, deve usare per forza il navmesh agent e fermarsi quando torna sull'ultima linea.
    //> Se una delle due condizioni di cui sopra è vera: return. Dunque... Qual è la domanda? Invalid line;
    //> Altrimenti, prosegui

    public void EvaluateMovement() //Movement, più che position
    {
        //hitInfo?
        RaycastHit hitInfo;

        //1. SphereCast verso Target > Per vedere se ha strada libera verso il target
        if (Physics.SphereCast(anim.GetBoneTransform(HumanBodyBones.Chest).position, agentNM.radius, transform.forward, out hitInfo, CombatDirector.DistanceInfo.LastLineRadius, raycastLayerMask)) //lasciare spherecast sulla chest?
        {
            var other = hitInfo;

            switch (other.collider.tag)
            {
                case "Player":
                    //Candidato = true;
                    //Push Forward(destinationLine)
                    isCandidate = true;
                    PushForward();
                    break;
                case "Enemy":                    
                    isCandidate = false;
                    EvaluatePosition(other);
                    break;
                default:
                    //Probabilmente hai hittato qualche altro ostacolo.
                    //Attiva il navmesh agent e rispozionati fino a quando non torni ad avere davanti o il Player o Enemy
                    break;
            }
        }
    }

    void PushToIdle()
    {
        state = AgentState.Idle;
        movementDir = AgentMovementDir.None;
        anim.SetTrigger(movingIdleHash);
    }

    void PushForward() //Ha la particolarità che dovrebbe raggiungere la destinazione, però :\
    {
        state = AgentState.Positioning;
        movementDir = AgentMovementDir.Forward;
        anim.SetTrigger(movingForwardHash);
    }

    void PushBackward()
    {
        state = AgentState.Positioning;
        movementDir = AgentMovementDir.Backward;
        anim.SetTrigger(movingBackwardHash);
    }
    void PushRight()
    {
        state = AgentState.Positioning;
        movementDir = AgentMovementDir.Right;
        anim.SetTrigger(movingRightHash);
    }
    void PushLeft()
    {
        state = AgentState.Positioning;
        movementDir = AgentMovementDir.Left;
        anim.SetTrigger(movingLeftHash);
    }

    void EvaluatePosition(RaycastHit other)
    {
        var distance = Vector3.Distance(transform.position, other.transform.position);
        var lines = Mathf.RoundToInt(distance / CombatDirector.DistanceInfo.LineToLineDistance);
        //Check distance e controlla se c'è spazio a sufficienza per il passaggio a 1 o più linee successive.
        //Dovresti controllare anche le intenzioni dell'enemy davanti a te.
        //Cosa sta facendo?
        //> È fermo: avanza
        //> Sta retrocedendo: se sta occupando la linea di destinazione, resta sulla tua linea e guardati ai lati
        //> Si sta spostando ai lati: aspetta o spostati dalla parte opposta.
        //In caso contrario, SphereCast attorno a te. Controlla a destra, a sinistra.
        //Controlla sempre le intenzioni degli altri, prima di muoverti.
        switch (lines)
        {
            case 0:
                //Valuta destra e sinistra (!canBackward)
                break;
            case 1:
                AgentAI otherAgnetAI = other.transform.GetComponent<AgentAI>();
                AgentMovementDir otherMovementDir = otherAgnetAI.movementDir;
                if (movementDir == AgentMovementDir.Backward)
                {
                    //valuta destra, sinistra, dietro (canBackward)
                }
                else
                {
                    nextLine = currentLine - 1;
                    PushForward(); //TO DESTINATION!!!
                }
                break;            
            default:
                nextLine = currentLine - (lines - 1);
                PushForward(); //To destination? Non dovrebbe valutare a ogni step? Mi sa che ci vuole un altro stato \ funzione... Che è lo stato in cui versano i possibili candidati (ma alcuni non lo saranno)
                break;
        }
    }

    void CheckSurrounds(Vector3 normal, bool canBackward)
    {
        //ha bisogno in entrata...
        //LA NORMALE DEL RAYCAST HIT, così sa se deve guardare prima da una parte o dall'altra.
        //Se può retrocedere o meno

        //Array di SphereCastAll, prendendo tutte le robe nella layerMask creata (dove ci saranno Enemies ed Env)
        //In base alla normale, guardertà partendo da destra o da sinistra, escludendo un angolo di 45° orientato verso la forward
        //Se non ci sono nemici entro 45° dalla direzione... bella, puoi andarci. Altrimenti Devi valutare dall'altra parte.
        //Se anche dall'altra parte non puoi fare nulla, entra in gioco canBackward.
        //Se puoi retrocedere, retrocedi
        //Se non puoi retrocedere, resti in idle (o fai positioning fermo per n secondo per evitare di fare controlli in continuazione? bisogna vedere le prestazioni).

        RaycastHit[] colliders = Physics.SphereCastAll(transform.position, agentNM.radius, Vector3.forward, 0f, raycastLayerMask);

        var angle = Vector3.Angle(transform.forward, normal);

        if (angle >= 0)
        {
            //parti da right
        }
        else
        {
            //parti da left
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
