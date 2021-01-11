using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

public enum AgentState { Idle, Positioning, Dispatching, Attacking, Retreat, Recover } //Vedere se mettere uno stato di trasferimento da una linea all'altra, che controlla solo se di è giunta la destinazione. Dovrebbe essere una derivata del positioning, però.
public enum AgentMovementDir { None, Forward, Backward, Right, Left }

[SelectionBase]
public class AgentAI : MonoBehaviour
{
    public      AgentState          state           = AgentState.Idle;
    public      AgentMovementDir    movementDir     = AgentMovementDir.None;

    //Rigidbody       sphereRigidbody;
    Rigidbody       chestRigidbody;
    NavMeshAgent    agentNM;
    Animator        animator;
    //CapsuleCollider capsuleCollider;
    SphereCollider avoidanceCollider;

    int movingIdleHash;
    int movingForwardHash;
    int movingBackwardHash;
    int movingRightHash;
    int movingLeftHash;
    int runningForwardHash;

    float idleTimer = 0;
    float idleTime = 0;
    float positioningTimer = 0;
    float stunTimer = 0;

    public      float       attackRange         =   1.5f;
    public      LayerMask   raycastLayerMask;
    public      Vector2     idleTimeMinMax      =   new Vector2(1, 3);
    public      float       positioningTime     =   1.5f;
    public      float       stunTime            =   2f;

    Transform target;

    public GameObject UI_counter;

    public      int         currentLine         =   -1;
    public      int         destinationLine     =   1;

    public bool canAttack = false;


    //vedere le string to hash.
    public bool isCandidate;

    //RaycastHit hitInfo;

    Vector3 rootMotion;

    Transform rootBone;
    List<Rigidbody> ragdollBodyParts;

    const float referenceAngle = 45f;
    const float straightAngle = 180f;

    public ParticlesManager particleManager;
    public GameObject distorsionFX;
    public ParticleSystem hitFX;

    private void Awake()
    {
        //SetRigidBody();
        SetNavMeshAgent();
        SetAnimator();
        //capsuleCollider = GetComponent<CapsuleCollider>();
        SetAvoidanceCollider();

        UI_counter = Instantiate(UI_counter, animator.GetBoneTransform(HumanBodyBones.Head));
        SetUICounter(false);

        SetRagdollRigidBodies();

        //CounterUI = Instantiate(CounterUI, anim.GetBoneTransform(HumanBodyBones.Head));
        //SetCounterUI(false);
    }

    void SetNavMeshAgent()
    {
        agentNM = GetComponent<NavMeshAgent>();
        agentNM.updateRotation = false;
        agentNM.stoppingDistance = attackRange;
    }

    void SetAnimator()
    {
        animator = GetComponent<Animator>();

        movingIdleHash = Animator.StringToHash("Idle");
        movingForwardHash = Animator.StringToHash("Step Forward");
        movingBackwardHash = Animator.StringToHash("Step Backward");
        movingRightHash = Animator.StringToHash("Step Right");
        movingLeftHash = Animator.StringToHash("Step Left");
        runningForwardHash = Animator.StringToHash("Run Forward");
    }

    void SetAvoidanceCollider()
    {
        avoidanceCollider = GetComponent<SphereCollider>();
        avoidanceCollider.radius = agentNM.radius;
    }

    void SetRagdollRigidBodies()
    {
        ragdollBodyParts = new List<Rigidbody>();

        rootBone = animator.GetBoneTransform(HumanBodyBones.Hips);
        chestRigidbody = animator.GetBoneTransform(HumanBodyBones.Chest).GetComponent<Rigidbody>();

        Rigidbody[] bones = rootBone.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody bone in bones)
        {
            ragdollBodyParts.Add(bone);
            bone.isKinematic = true;
        }

    }

    private void OnEnable()
    {

    }

    private void Start()
    {
        target = CombatDirector.DistanceInfo.Target;
        CheckCurrentLine();
        ////TestRigidBody
        //rigidbody.AddForce(((transform.position - target.position).normalized + Vector3.up * .1f) * 70f, ForceMode.Impulse);
        //state = AgentState.Recover;
        //agentNM.enabled = false;
        //animator.SetTrigger("Stunned");
    }

    private void FixedUpdate()
    {
        //NewHandleMovement();
        switch (state) //Gli stati come Scriptable Object? Con dei simpatici override? Pensaci per refactoring
        {
            case AgentState.Idle:
                UpdateOnIdle();
                break;
            case AgentState.Positioning:
                UpdateOnPositioning();
                //Si sta muovendo e deve verificare se è arrivato alla linea target
                break;
            case AgentState.Dispatching:
                UpdateOnDispatching();
                //Sta correndo verso l'obiettivo
                break;
            case AgentState.Attacking:
                HandleMovement();
                HandleRotation();
                //Sono tutti in posizione e stanno attaccando
                break;
            case AgentState.Retreat:
                UpdateOnRetreat();
                //È alla linea 0 o ha finito di attaccare
                break;
            case AgentState.Recover:
                //è a terra e deve rialzarsi
                OnUpdateRecover();
                break;
            default:
                break;
        }
    }

    private void OnAnimatorMove()
    {
        rootMotion += animator.deltaPosition;
    }

    Vector3 DistanceToTarget()
    {
        return Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
    }

    #region Handle Movement and Rotation

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

    #endregion

    #region Update Idle State

    void UpdateOnIdle()
    {
        CheckCurrentLine();
        if (!IsValidLine()) return;

        HandleMovement();
        HandleRotation();

        idleTimer += Time.fixedDeltaTime;
        
        if (idleTimer > idleTime)
        {
            idleTimer = 0;
            animator.ResetTrigger(movingIdleHash); //di sicurezza, dato che può entrare qui da idle e da positioning
            EvaluateMovement(); //Vedere se return type o spostare effetivamente per ultima
        }
    }
    #endregion

    #region Update Position State

    void UpdateOnPositioning() //Ci sono due modalità: raggiungi la destinazione o continua per tot tempo
    {
        CheckCurrentLine();
        if (!IsValidLine()) return;

        HandleMovement();
        HandleRotation();

        switch (movementDir) //Gestisce i time?
        {
            case AgentMovementDir.None:
                PushToIdle();
                break;
            case AgentMovementDir.Forward:
                if (agentNM.hasPath)
                {
                    if (NavMeshDestinationReached())
                    {
                        animator.ResetTrigger(movingForwardHash);
                        PushToIdle();
                    }
                    return;
                }

                EvaluateMovement();
                if (currentLine == destinationLine)
                {
                    animator.ResetTrigger(movingForwardHash);
                    PushToIdle();
                }
                break;
            case AgentMovementDir.Backward:
                //Controlli
                if (currentLine == destinationLine) //Potrebbe dare problemi col dispatch
                    PushToIdle();
                break;
            //case AgentMovementDir.Right:
            //    //If timer > maxSideStepTime...
            //    break;
            //case AgentMovementDir.Left:
            //    //If time > maxSideStepTime
            //    break;
            default:
                positioningTimer += Time.fixedDeltaTime;

                if (positioningTimer >= positioningTime)
                {                    
                    animator.ResetTrigger(movingRightHash);
                    animator.ResetTrigger(movingLeftHash);
                    positioningTimer = 0;
                    PushToIdle();
                }
                break;
        }
    }
    #endregion

    #region Update Dispatching State

    public void UpdateOnDispatching()
    {
        HandleMovement();
        HandleRotation();
        CheckCurrentLine();

        agentNM.SetDestination(target.position);
        if (!agentNM.hasPath) return;


        if (!NavMeshDestinationReached())
        {
            canAttack = false;
            animator.SetBool("Can Attack", canAttack);
            return;
        }

        animator.ResetTrigger(runningForwardHash);
        animator.SetTrigger("Ready");
        animator.SetBool("Can Attack", canAttack);
        movementDir = AgentMovementDir.None;
        canAttack = true;
    }
    #endregion

    #region Attack State
    public void Attack()
    {
        state = AgentState.Attacking;
        canAttack = false; //LASCIALO A TRUE
        //canAttack torna a false all'animation Event o quando contrattaccato
        //In questo modo, possiamo fare degli aggiustamenti di posizione e rotazione durante lo stato attack PRIMA dell'impatto del colpo.
        isCandidate = false;
        animator.SetBool("Can Attack", canAttack);
        animator.ResetTrigger("Ready");
        CheckCurrentLine();
        //Debug.Break();
        animator.SetTrigger("Attack");
        SetUICounter(true);
    }
    #endregion

    #region Update Retrat
    void UpdateOnRetreat()
    {
        CheckCurrentLine();
        if (currentLine == destinationLine)
        {
            PushToIdle();
        }

        HandleMovement();
        HandleRotation();
    }
    #endregion

    #region Update Recover
    void OnUpdateRecover() //Funzionicchia
    {
        stunTimer += Time.fixedDeltaTime;
        if (stunTimer >= stunTime)
        {
            //Debug.Break();
            if (agentNM.isActiveAndEnabled)
            {
                HandleMovement();
            }
            else
            {
                EvaluateForward();
                transform.position = rootBone.position;
                transform.rotation = rootBone.rotation;
                SetRagdollKinematicRigidbody(true);
                animator.enabled = true;
                //animator.Rebind();
                agentNM.enabled = true;
                avoidanceCollider.enabled = true;
                animator.SetTrigger("Getting Up");
            }
        }
    }
    #endregion

    void SetRagdollKinematicRigidbody(bool boolean)
    {
        foreach (Rigidbody body in ragdollBodyParts)
        {
            body.isKinematic = boolean;
        }
    }

    void EvaluateForward()
    {
        if (rootBone.forward.y >= 0)
        {
            animator.SetTrigger("From Back");
            rootBone.rotation.SetLookRotation(Vector3.up);
        }
        else
        {
            animator.SetTrigger("From Front");
            rootBone.rotation.SetLookRotation(Vector3.down);
        }
    }

    #region Movement Triggers
    void PushToIdle()
    {
        state = AgentState.Idle;
        movementDir = AgentMovementDir.None;
        idleTime = Random.Range(idleTimeMinMax.x, idleTimeMinMax.y);
        animator.SetTrigger(movingIdleHash);
        //Debug.Break();
    }

    void PushForward(int _destinationLine)
    {
        _destinationLine = destinationLine;
        if (currentLine > 1)
        {
          state = AgentState.Positioning;
          movementDir = AgentMovementDir.Forward; 
          //animator.ResetTrigger(movingIdleHash);
          animator.SetTrigger(movingForwardHash);
        }
        //else
        //{
        //    PushToIdle();
        //}
    }
    void PushBackward(int _destinationLine)
    {
        destinationLine = _destinationLine;
        state = AgentState.Retreat;
        movementDir = AgentMovementDir.Backward;
        animator.ResetTrigger(movingIdleHash);
        animator.SetTrigger(movingBackwardHash);
    }
    void PushRight()
    {
        state = AgentState.Positioning;
        movementDir = AgentMovementDir.Right;
        animator.ResetTrigger(movingForwardHash);
        animator.ResetTrigger(movingBackwardHash);
        animator.SetTrigger(movingRightHash);
    }
    void PushLeft()
    {
        state = AgentState.Positioning;
        movementDir = AgentMovementDir.Left;
        animator.ResetTrigger(movingForwardHash);
        animator.ResetTrigger(movingBackwardHash);
        animator.SetTrigger(movingLeftHash);
    }

    void RunForward(float _stoppingDistance)
    {
        agentNM.SetDestination(target.position);
        agentNM.stoppingDistance = _stoppingDistance;

        //state = AgentState.Dispatching; //Dispatching lo chiama solo l'event! Di base, è positioning
        state = AgentState.Positioning; //Dispatching lo chiama solo l'event! Di base, è positioning
        movementDir = AgentMovementDir.Forward;
        animator.ResetTrigger(movingIdleHash);
        animator.SetTrigger(runningForwardHash);
    }
    #endregion

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

    void CheckCurrentLine()
    {
        int oldLine = currentLine;
        currentLine = CombatDirector.DistanceInfo.GetLine(this.transform);
        if (currentLine != oldLine)
        {
            CombatDirector.ChangeLineList(this, oldLine, currentLine);
        }
    }

    bool IsValidLine()
    {
        if (currentLine == 0)
        {
            PushBackward(1);
            return false;
        }
        else
        if (currentLine > CombatDirector.DistanceInfo.Lines)
        {
            RunForward(CombatDirector.DistanceInfo.LastLineRadius);
            return false;
        }
        //Se torna in idle, fa uno step per volta.
        return true;
    }

    bool NavMeshDestinationReached()
    {
        //DEBUGGER PATH
        Debug.DrawRay(transform.position + Vector3.up, agentNM.desiredVelocity, Color.blue);
        for (int i = 0; i < agentNM.path.corners.Length - 1; i++)
        {
            Debug.DrawLine(agentNM.path.corners[i], agentNM.path.corners[i + 1], Color.cyan, 0.02f);
        }

        //Debug.Log(DistanceToTarget().magnitude);

        if (DistanceToTarget().magnitude > agentNM.stoppingDistance) //cambia stopping distance
        {
            return false;
        }
        return true;
    }

    RaycastHit testHit;

    public void EvaluateMovement() //Movement, più che position
    {
        //hitInfo?
        RaycastHit hitInfo;

        //Vector3 raycastOrigin = animator.GetBoneTransform(HumanBodyBones.Chest).position;
        Vector3 raycastOrigin = transform.position + Vector3.up;

        //1. SphereCast verso Target > Per vedere se ha strada libera verso il target
        if (Physics.SphereCast(raycastOrigin, agentNM.radius, transform.forward, out hitInfo, CombatDirector.DistanceInfo.LastLineRadius, raycastLayerMask)) //lasciare spherecast sulla chest?
        {
            var other = hitInfo;
            testHit = hitInfo;

            switch (other.transform.root.tag) //forse va trovata un'altra soluzione. Bisognerebbe avere le reference delle collisioni nell'oggetto padre, no?
            {
                case "Player":
                    isCandidate = true;
                    PushForward(1);
                    break;
                case "Foe":
                    isCandidate = false;
                    EvaluatePosition(other);
                    break;
                default:
                    RunForward(CombatDirector.DistanceInfo.LineDistance(currentLine)); //C'è bisogno di una funziona che dia il raggio della linea corrente
                    break;
            }
        }
    }

    void EvaluatePosition(RaycastHit other)
    {
        //Check distance e controlla se c'è spazio a sufficienza per il passaggio a 1 o più linee successive.
        //Dovresti controllare anche le intenzioni dell'enemy davanti a te.
        //Cosa sta facendo?
        //> È fermo: avanza
        //> Sta retrocedendo: se sta occupando la linea di destinazione, resta sulla tua linea e guardati ai lati
        //> Si sta spostando ai lati: aspetta o spostati dalla parte opposta.
        //In caso contrario, SphereCast attorno a te. Controlla a destra, a sinistra.
        //Controlla sempre le intenzioni degli altri, prima di muoverti.
        var distance = Vector3.Distance(transform.position, other.transform.position);
        var lines = Mathf.RoundToInt(distance / CombatDirector.DistanceInfo.LineToLineDistance);
        switch (lines)
        {
            case 0:
                CheckSurrounding(other.normal, true); //O true, o retreat
                break;
            case 1: //Deve sapere le intenzioni degll'altro per capire se deve retrocedere o meno.
                AgentAI otherAgnetAI = other.transform.root.GetComponent<AgentAI>();
                AgentMovementDir otherMovementDir = otherAgnetAI.movementDir;
                if (otherMovementDir == AgentMovementDir.Backward)
                {
                    CheckSurrounding(other.normal, true);
                }
                else
                {
                    CheckSurrounding(other.normal, false);
                }
                break;
            default:
                PushForward(currentLine - (lines - 1));
                break;
        }
    }

    void CheckSurrounding(Vector3 normal, bool canBackward) //Questa funzione è una merda. Va rivista
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

        avoidanceCollider.enabled = false; //Lo spherecast prende anche sé stesso, sennò!
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, agentNM.radius * 3.5f, Vector3.forward, 0f, raycastLayerMask);
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
                PushToIdle();
                return;
            }
        }

        if (angleSign > 0)
        {
            if (canGoRight)
            {
                PushRight();
                return;
            }
            else
            if (canGoLeft)
            {
                PushLeft();
                return;
            }
        }
        else
        {
            if (canGoLeft)
            {
                PushLeft();
                return;
            }
            else
            if (canGoRight)
            {
                PushRight();
                return;
            }
        }
        if (canBackward)
        {
            PushBackward(currentLine + 1); //meglio metterla come parametro del metodo? In questo modo, si è sicuri di non dimenticare mai di settare la destinazione
        }
    }

    public void DispatchEvent() //Chiamata dal combat director, come un event
    {
        RunForward(attackRange);
        //Override Active Animator Triggers
        animator.ResetTrigger(movingForwardHash);
        animator.ResetTrigger(runningForwardHash);
        //Set Dispatch state
        state = AgentState.Dispatching;
        destinationLine = 0;
        animator.SetTrigger("Dispatch");
    }

    public void OnAttackAnimationEvent()
    {
        agentNM.ResetPath();

        RaycastHit hitInfo;
        if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out hitInfo, attackRange))
        {
            target.GetComponent<Player>().Stun();

            particleManager.OnPlayerHit(hitInfo.point);
        }
        SetUICounter(false);
        PushToIdle();
    }

    public void Stun()
    {
        animator.SetTrigger("Stunned");
    }

    

    public void SetUICounter(bool boolean)
    {
        UI_counter.SetActive(boolean);
    }

    public void ResetNavMeshPath()
    {
        agentNM.ResetPath();
    }

    public void Hit() //Passare la forza!
    {
        avoidanceCollider.enabled = false;        
        agentNM.enabled = false;
        animator.enabled = false;
        if (CombatDirector.strikers.Contains(this))
        {
            CombatDirector.strikers.Remove(this);
            SetUICounter(false);
        }
        if (CombatDirector.strikers.Count == 0)
        {
            CombatDirector.state = CombatDirectorState.Planning;
        }
        //Attiva i rigidbody della ragdoll
        SetRagdollKinematicRigidbody(false);
        chestRigidbody.AddForce(((transform.position - target.position).normalized + Vector3.up * .1f) * 70f, ForceMode.Impulse);
        StartCoroutine(TestFX());
        Instantiate(distorsionFX.gameObject, chestRigidbody.transform);
        stunTimer = 0;
        state = AgentState.Recover;
    }

    IEnumerator TestFX()
    {
        distorsionFX.SetActive(true);
        distorsionFX.transform.position = chestRigidbody.transform.position;

        float timer = 0;
        distorsionFX.transform.localScale = new Vector3(.5f, .5f, .5f);

        while (timer < 1f)
        {
            timer += Time.fixedDeltaTime;
            distorsionFX.transform.localScale *= .1f;
            yield return new WaitForFixedUpdate();
        }

        distorsionFX.SetActive(false);
    }

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

    private void OnDrawGizmos()
    {
        if (testHit.transform != null)
        {
            Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * testHit.distance);
            Gizmos.DrawWireSphere(transform.position + Vector3.up + transform.forward * testHit.distance, agentNM.radius);
        }
        
    }
}
