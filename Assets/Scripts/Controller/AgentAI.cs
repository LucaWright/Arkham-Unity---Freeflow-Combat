using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

public enum AgentState { Idle, Positioning, Dispatching, Attacking, Retreat, Stun, Hit, Recover } //Vedere se mettere uno stato di trasferimento da una linea all'altra, che controlla solo se di è giunta la destinazione. Dovrebbe essere una derivata del positioning, però.
public enum AgentMovementDir { None, Forward, Backward, Right, Left }

[SelectionBase]
public class AgentAI : MonoBehaviour
{
    public      AgentState          state           = AgentState.Idle;
    public      AgentMovementDir    movementDir     = AgentMovementDir.None;

    //STATES
    public StateMachine fsm;
    [HideInInspector] public AIIdle idleState;
    [HideInInspector] public AIPositioning positioningState;
    [HideInInspector] public AIDispatching dispatchingState;
    [HideInInspector] public AIAttacking attackingState;
    [HideInInspector] public AIRetrat retreatState;
    [HideInInspector] public AIOnHit hitState;
    [HideInInspector] public AIOnStun stunState;
    [HideInInspector] public AIRecover recoverState;

    //AGENT COMPONENTS
    [HideInInspector] public Rigidbody       chestRigidbody;
    [HideInInspector] public NavMeshAgent    agentNM;
    [HideInInspector] public Animator        animator;
    [HideInInspector] public SphereCollider avoidanceCollider;

    //NAVMESHAGENT SETTINGS
    [HideInInspector] public float agentNMradius;

    //ANIMATOR SETTINGS
    [HideInInspector] public int idleHash;
    [HideInInspector] public int movingForwardHash;
    [HideInInspector] public int movingBackwardHash;
    [HideInInspector] public int movingRightHash;
    [HideInInspector] public int movingLeftHash;
    [HideInInspector] public int runningForwardHash;
    [HideInInspector] public int stopHash;


    [HideInInspector] public     float       attackRange         =   1.5f;                //LOCAL
    public     LayerMask   agentLineOfSightLM;                                     //LOCAL?
 //public     Vector2     idleTimeMinMax      =   new Vector2(1, 3);   //LOCAL

    [HideInInspector] public Transform target;

    /*[HideInInspector]*/ public      int         currentLine         =   -1;                 //GLOBAL => Get Set?
    /*[HideInInspector]*/ public      int         destinationLine     =   1;                  //GLOBAL => Get Set?

    /*[HideInInspector]*/ public bool canAttack = false;                                      //GLOBAL
    /*[HideInInspector]*/ public bool isCandidate;                                            //Potenziale cambio di logica

    [HideInInspector] public Vector3 rootMotion;                                          //GLOBAL

    [HideInInspector] public Transform rootBone;
    [HideInInspector] public List<Rigidbody> ragdollBodyParts;
    [HideInInspector] public List<RagdollSnapshot> ragdollSnapshot;

    [HideInInspector] public Transform chestTransf;
    [HideInInspector] public Vector3 impulseForce;

    public bool hasGreenLightToTarget;

    public const float referenceAngle = 45f;
    public const float straightAngle = 180f;

    public Vector3 averageVelocityVector = Vector3.zero; //TODO: tiene traccia dei movimenti e della direction degli stessi!
    public Vector3 desiredDir = Vector3.zero;

    //
    public float minMovementMagnitudue = .35f;

    Vector3[] rawDirections;
    //public Vector3[] contextDirection;
    float[] dangerMap;
    float[] interestMap;
    float[] interestMapWeights;



    private void Awake()
    {
        agentNM = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        avoidanceCollider = GetComponent<SphereCollider>();
        fsm = new StateMachine();
    }

    private void OnEnable()
    {
        SetNavMeshAgent();
        SetAnimator();
        SetAvoidanceCollider();
        SetRagdollRigidBodies();
        SetRagdollSnapshotSystem();
        SetFiniteStateMachine(); //a parte fsm, può andare in start
        SetAvoidanceSystem();
    }

    private void Start()
    {
        target = CombatDirector.DistanceInfo.Target;
        CheckCurrentLine();
        fsm.State = idleState;
    }

    private void Update()
    {
        CheckCurrentLine();
        fsm.State.OnUpdate();        
    }
    private void FixedUpdate()
    {
        fsm.State.OnFixedUpdate();
    }

    private void LateUpdate()
    {
        fsm.State.OnLateUpdate();
    }

    public void OnCollisionEnter(Collision collision)
    {
        //Possibile utilizzo
    }

    private void OnAnimatorMove()
    {
        rootMotion += animator.deltaPosition;
    }

    #region AWAKE SETTINGS
    void SetNavMeshAgent()
    {        
        agentNM.updateRotation = false;
        agentNM.stoppingDistance = attackRange;
        agentNMradius = agentNM.radius;
    }
    void SetAnimator()
    {  
        idleHash = Animator.StringToHash("Idle");
        movingForwardHash = Animator.StringToHash("Step Forward");
        movingBackwardHash = Animator.StringToHash("Step Backward");
        movingRightHash = Animator.StringToHash("Step Right");
        movingLeftHash = Animator.StringToHash("Step Left");
        runningForwardHash = Animator.StringToHash("Dispatch");
        stopHash = Animator.StringToHash("Stop");
    }
    public void SetAvoidanceCollider()
    {        
        avoidanceCollider.radius = agentNM.radius;
    }
    public void SetRagdollRigidBodies()
    {
        ragdollBodyParts = new List<Rigidbody>();

        rootBone = animator.GetBoneTransform(HumanBodyBones.Hips);
        chestRigidbody = animator.GetBoneTransform(HumanBodyBones.Chest).GetComponent<Rigidbody>();
        chestTransf = chestRigidbody.transform;

        Rigidbody[] bones = rootBone.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody bone in bones)
        {
            ragdollBodyParts.Add(bone);
            bone.isKinematic = true;
        }
    }
    public void SetRagdollSnapshotSystem()
    {
        ragdollSnapshot = new List<RagdollSnapshot>();
        
        Transform[] transforms = rootBone.GetComponentsInChildren<Transform>();
        foreach (Transform transform in transforms)
        {
            RagdollSnapshot snapshot = new RagdollSnapshot();
            snapshot.transform = transform;
            ragdollSnapshot.Add(snapshot);
        }
    }

    void SetFiniteStateMachine() //è lui che prepara i vari stati inserendo script e fsm?
    {
        idleState = GetComponent<AIIdle>();
        positioningState = GetComponent<AIPositioning>();
        dispatchingState = GetComponent<AIDispatching>();
        attackingState = GetComponent<AIAttacking>();
        retreatState = GetComponent<AIRetrat>(); //retreating?
        stunState = GetComponent<AIOnStun>(); //retreating?
        hitState = GetComponent<AIOnHit>(); //retreating?
        recoverState = GetComponent<AIRecover>(); //recovering?        
    }

    void SetAvoidanceSystem()
    {
        rawDirections = new Vector3[]
        {
            Vector3.forward,
            Vector3.right,
            - Vector3.right,
            - Vector3.forward
        };
        dangerMap = new float[rawDirections.Length];
        interestMap = new float[rawDirections.Length];
        interestMapWeights = new float[]
        {
            1f,
            .7f,
            .7f,
            .5f
        };
    }
    #endregion

    public void CheckCurrentLine()
    {
        int oldLine = currentLine;
        currentLine = CombatDirector.DistanceInfo.GetLine(this.transform);
        if (currentLine != oldLine)
        {
            CombatDirector.UpdateAgentLists(this, oldLine, currentLine);
            CombatDirector.UpdateCandidateLists(this, oldLine, currentLine);
        }
    }

    public bool IsValidLine()
    {
        if (currentLine == 0)
        {
            //StopAllCoroutines();
            //StartCoroutine(PullBack(1));
            PullBack(1);
            return false;
        }
        else
        if (currentLine > CombatDirector.DistanceInfo.Lines) //TODO: funzione dispatching verso ultima linea!
        {
            RunForward(CombatDirector.DistanceInfo.LastLineRadius);
            fsm.State = dispatchingState;
            return false;
        }
        return true;
    }
    public bool HasGreenLightToTheTarget() //return bool???
    {
        desiredDir = transform.forward;        
        RaycastHit? hit = CheckTowardsDir(transform.forward);
        if (hit.HasValue)
        {
            var hitInfo = (RaycastHit)hit;

            if (hitInfo.transform.tag == "Player")
            {
                //CombatDirector.AddToStrikersCandidateList(this, currentLine); //TODO: verificare, perché potrebbe generare errori. DOVREBBE FARLA I COMBAT DIRECTOR!!
                hasGreenLightToTarget = true;
                return hasGreenLightToTarget;
            }
            else
            if (hitInfo.transform.tag == "Enemy")
            {
                //Desired direction nel caso hitti un Enemy! Così può valutarla! TODO HIGH PRIORITY
                //desiredDir += transform.right * Vector3.Dot(transform.right, hitInfo.normal);
                desiredDir += hitInfo.normal;
            }
            else
            {
                //C'è un ostacolo da evitare. Deve andare in navmesh
            }


        }
        hasGreenLightToTarget = false;
        return hasGreenLightToTarget;
    }
    private RaycastHit? CheckTowardsDir(Vector3 direction) //RIPETUTA! DRY!
    {
        Vector3 raycastOrigin = transform.position + Vector3.up;
        RaycastHit hitInfo;
        if (Physics.SphereCast(raycastOrigin, agentNM.radius, direction, out hitInfo, CombatDirector.DistanceInfo.LastLineRadius, agentLineOfSightLM)) //lasciare spherecast sulla chest?
        {
            return hitInfo;
        }
        return null;
    }
    public void CheckSurroundings() //bool??? Ma la domanda è: può muoversi?
    {
        avoidanceCollider.enabled = false;
        Collider[] characterColliders = Physics.OverlapSphere(avoidanceCollider.transform.position, CombatDirector.DistanceInfo.LineToLineDistance, agentLineOfSightLM); //vedere poi per raggio
        avoidanceCollider.enabled = true;

        int characterArrayLength = characterColliders.Length;

        if (characterArrayLength > 0)
        {
            ResetAllAvoidanceMaps();
            for (int i = 0; i < characterArrayLength; i++)
            {
                Vector3 distanceVector = characterColliders[i].transform.position - transform.position;
                UpdateDangerMap(distanceVector);
                //rawDirection += distanceVector.normalized / distanceVector.magnitude * CombatDirector.DistanceInfo.LineToLineDistance; //Vettore minimo = 1;
            }
            //EvaluateDirection(rawDirection);
            EvaluateMovement();
        }
        else
        {
            //StartCoroutine(PushForward(currentLine - 1));
            PushForward(currentLine - 1);
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
            interestMap[i] = 0; //inizializzarle già ad 1? 
        }
    }
    void UpdateDangerMap(Vector3 obstacle)
    {
        for (int i = 0; i < rawDirections.Length; i++)
        {
            float dot = Vector3.Dot(obstacle, WorldToLocalVector(rawDirections[i]));
            if (dot > 0)
            {
                dangerMap[i] += Vector3.Dot(obstacle, WorldToLocalVector(rawDirections[i]));
            }
        }
    }

    Vector3 WorldToLocalVector(Vector3 _vector)
    {
        return _vector.z * transform.forward + _vector.x * transform.right + _vector.y * transform.up;
    }

    void EvaluateMovement() //return type
    {
        float interest = 0f;
        int index = 0;

        for (int i = 0; i < dangerMap.Length; i++)
        {
            interestMap[i] = Mathf.Clamp01(1 - dangerMap[i]) * interestMapWeights[i]; //Già pesato

            if (interestMap[i] > interest) //Cerca l'interesse più alto
            {
                interest = interestMap[i];
                index = i;
            }
        }

        desiredDir = (WorldToLocalVector(rawDirections[index]) * interestMap[index]);
        //desiredDir = desiredDir.sqrMagnitude >= Mathf.Pow(minMovementMagnitudue, 2) ? desiredDir : Vector3.zero;
        if (desiredDir.sqrMagnitude >= Mathf.Pow(minMovementMagnitudue, 2))
        {
            switch (index)
            {
                case 0:
                    PushForward(currentLine - 1);
                    break;
                case 1:
                    StepRight();
                    break;
                case 2:
                    StepLeft();
                    break;
                case 3:
                    PullBack(currentLine + 1);
                    break;
            }
        }
        else
        if (state != AgentState.Idle)
        {
            BackToIdle();
        }        
    }

    //void EvaluateDirection(Vector3 rawDirection)
    //{
    //    if (rawDirection.magnitude < 1.4f) //la magnitude ci dice anche  se deve stare fermo! Mica ci dobbiamo muovere per forza!
    //    {
    //        return;
    //    }

    //    Debug.DrawRay(avoidanceCollider.transform.position, rawDirection, Color.green);
    //    float angle = Vector3.SignedAngle(transform.forward, rawDirection.normalized, Vector3.up); //Senza normalizzazione, diventa un parametro che indica "l'urgenza". Tramite animation blending si può regolare la velocità.

    //    if (Mathf.Abs(angle) <= referenceAngle / 2f)
    //    {
    //        //StartCoroutine(PushForward(currentLine - 1));
    //        PushForward(currentLine - 1);
    //    }
    //    else
    //    if (Mathf.Abs(angle) > straightAngle - referenceAngle / 2f)
    //    {
    //        //StartCoroutine(PullBack(currentLine + 1));
    //        PullBack(currentLine + 1);
    //    }
    //    else
    //    if (angle > referenceAngle / 2f)
    //    {
    //        //StartCoroutine(StepRight());
    //        StepRight();
    //    }
    //    else
    //    {
    //        //StartCoroutine(StepLeft());
    //        StepLeft();
    //    }
    //}

    public void HandleRootMotionMovement() //TODO: controllare se entra ancora in questo break
    {
        if (agentNM.isActiveAndEnabled)
        {
            agentNM.Move(rootMotion);
        }
        else
        {
            agentNM.Move(rootMotion);
            Debug.Break();
        }
        rootMotion = Vector3.zero;
    }
    public void HandleRootMotionRotation()
    {
        var towardsDirection = (target.position - transform.position); //mettere funzione di calcolo
        Quaternion lookRotation = Quaternion.LookRotation(towardsDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, .1f);
    }

    public void HandleNavMeshMovement()
    {
        agentNM.velocity = rootMotion / Time.fixedDeltaTime;
        agentNM.speed = agentNM.velocity.magnitude;
        rootMotion = Vector3.zero;
    }
    public void HandleNavMeshRotation()
    {
        var towardsDirection = agentNM.desiredVelocity != Vector3.zero ? agentNM.desiredVelocity : (target.position - transform.position); //funzione calcolo
        Quaternion lookRotation = Quaternion.LookRotation(towardsDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, .25f);
    }

    #region MOVEMENT TRIGGERS

    //public IEnumerator BackToIdle() //Change State to Idle at animation transition END.
    //{
    //    movementDir = AgentMovementDir.None;
    //    animator.SetTrigger(idleHash);

    //    //Se è già in transition, aspetta...
    //    while (animator.IsInTransition(0))
    //    {
    //        yield return new WaitForFixedUpdate();
    //    }
    //    //Dopodiché, aspetta la prossima transizione...

    //    while (!animator.IsInTransition(0))
    //    {
    //        yield return new WaitForFixedUpdate();
    //    }
    //    yield return new WaitForSeconds(animator.GetAnimatorTransitionInfo(0).duration);
    //    fsm.State = idleState;
    //}

    public void BackToIdle()
    {
        movementDir = AgentMovementDir.None;
        animator.SetTrigger(idleHash);
        fsm.State = idleState;
    }

    //public IEnumerator PushForward(int _destinationLine) //Change State to Positioning at animation transition START.
    //{
    //    destinationLine = Mathf.Max(1, _destinationLine); //Secutiry Check

    //    if (currentLine == 1) yield break;

    //    movementDir = AgentMovementDir.Forward;
    //    animator.SetTrigger(movingForwardHash);
    //    //Se è già in transition, aspetta...
    //    while (animator.IsInTransition(0))
    //    {
    //        yield return new WaitForFixedUpdate();
    //    }
    //    //Dopodiché, aspetta la prossima transizione...
    //    while (!animator.IsInTransition(0))
    //    {
    //        yield return new WaitForFixedUpdate();
    //    }
    //    fsm.State = positioningState;
    //}

    public void PushForward(int _destinationLine)
    {
        destinationLine = Mathf.Max(1, _destinationLine); //Secutiry Check
        if (currentLine == 1) //Security check? TODO: verifica chi fa questo controllo
        {
            BackToIdle();
            return;
        }

        movementDir = AgentMovementDir.Forward;
        animator.SetTrigger(movingForwardHash);
        fsm.State = positioningState;
    }

    //public IEnumerator PullBack(int _destinationLine) //Change State to Retreat at animation transition START.
    //{
    //    //TODO
    //    //Potenziale problema: entra qui dentro durante la transizione di un altro stato. Di conseguenza...
    //    //Va in backward finita la transizione di un altro trigger e non di questo.
    //    destinationLine = _destinationLine;
    //    //if (currentLine >= destinationLine)
    //    //{
    //    //    yield break;
    //    //}           

    //    fsm.State = retreatState;
    //    movementDir = AgentMovementDir.Backward;
    //    animator.SetTrigger(movingBackwardHash); //Non spiega, però, perché questo trigger venga resettato. A meno che... non siano gli altri stati a farlo.

    //    yield break;

    //    ////Se è già in transition, aspetta...
    //    //while (animator.IsInTransition(0))
    //    //{
    //    //    yield return new WaitForFixedUpdate();
    //    //}
    //    ////Dopodiché, aspetta la prossima transizione...
    //    //while (!animator.IsInTransition(0))
    //    //{
    //    //    yield return new WaitForFixedUpdate();
    //    //}        
    //}

    public void PullBack(int _destinationLine)
    {
        destinationLine = _destinationLine;
        movementDir = AgentMovementDir.Backward;
        animator.SetTrigger(movingBackwardHash);
        fsm.State = retreatState;
    }

    //public IEnumerator StepRight() //Change State to Positioning at animation transition START.
    //{
    //    movementDir = AgentMovementDir.Right;
    //    animator.SetTrigger(movingRightHash);
    //    //Se è già in transition, aspetta...
    //    while (animator.IsInTransition(0))
    //    {
    //        yield return new WaitForFixedUpdate();
    //    }
    //    //Dopodiché, aspetta la prossima transizione...
    //    while (!animator.IsInTransition(0))
    //    {
    //        yield return new WaitForFixedUpdate();
    //    }
    //    fsm.State = positioningState;
    //}
    //public IEnumerator StepLeft() //Change State to Positioning at animation transition START.
    //{
    //    movementDir = AgentMovementDir.Left;
    //    animator.SetTrigger(movingLeftHash);
    //    //Se è già in transition, aspetta...
    //    while (animator.IsInTransition(0))
    //    {
    //        yield return new WaitForFixedUpdate();
    //    }
    //    //Dopodiché, aspetta la prossima transizione...
    //    while (!animator.IsInTransition(0))
    //    {
    //        yield return new WaitForFixedUpdate();
    //    }
    //    fsm.State = positioningState;
    //}

    public void StepRight()
    {
        movementDir = AgentMovementDir.Right;
        animator.SetTrigger(movingRightHash);
        fsm.State = positioningState;
    }

    public void StepLeft()
    {
        movementDir = AgentMovementDir.Left;
        animator.SetTrigger(movingLeftHash);
        fsm.State = positioningState;
    }

    public void RunForward(float _stoppingDistance) //Stop all current States.
    {
        movementDir = AgentMovementDir.Forward;
        StopAllCoroutines(); //Perchè a volte non funziona?
        animator.ResetTrigger(movingForwardHash);
        animator.SetTrigger(runningForwardHash);

        agentNM.SetDestination(target.position);
        agentNM.stoppingDistance = _stoppingDistance;
    }
    #endregion    

    public bool NavMeshDestinationReached()
    {
        //DEBUGGER PATH
        //Debug.DrawRay(transform.position + Vector3.up, agentNM.desiredVelocity, Color.blue);
        for (int i = 0; i < agentNM.path.corners.Length - 1; i++)
        {
            Debug.DrawLine(agentNM.path.corners[i], agentNM.path.corners[i + 1], Color.cyan, 0.02f);
        }

        //Debug.Log(DistanceToTarget().magnitude);

        if (DistanceToTarget().magnitude > agentNM.stoppingDistance) //cambia stopping distance
        {
            return false;
        }
        //ResetNavMeshPath();
        return true;
    }
    Vector3 DistanceToTarget()
    {
        return Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
    }

    public void OnStun()
    {
        StopAllCoroutines();
        fsm.State = stunState;
    }    

    public void SetUICounterActive(bool boolean)
    {
         attackingState.SetUICounterActive(boolean);
    }

    public void ResetNavMeshPath()
    {
        if (agentNM.hasPath)
            agentNM.ResetPath();
    }

    public void StopRunning(bool boolean)
    {
        animator.SetBool(stopHash, boolean);
    }

    public void OnHit(Vector3 force) //Passare la forza!
    {
        ResetNavMeshPath();
        StopAllCoroutines();
        if (CombatDirector.strikers.Contains(this))
        {
            CombatDirector.strikers.Remove(this);
            SetUICounterActive(false);
        }
        impulseForce = force;
        fsm.State = hitState;
    }

    public void SetRagdollKinematicRigidbody(bool boolean)
    {
        foreach (Rigidbody body in ragdollBodyParts)
        {
            body.isKinematic = boolean;
        }
    }






    private void OnDrawGizmos()
    {
        //if (testHit.transform != null)
        //{
        //    Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * testHit.distance);
        //    Gizmos.DrawWireSphere(transform.position + Vector3.up + transform.forward * testHit.distance, agentNM.radius);
        //}
        
    }
}
