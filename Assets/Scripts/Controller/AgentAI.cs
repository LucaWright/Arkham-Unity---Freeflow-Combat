using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

public enum AgentState { Idle, Positioning, Locomotion, Repositioning, Dispatching, Attacking, Retreat, Stun, Hit, Recover } //Vedere se mettere uno stato di trasferimento da una linea all'altra, che controlla solo se di è giunta la destinazione. Dovrebbe essere una derivata del positioning, però.
public enum AgentMovementDir { None, Forward, Backward, Right, Left }

[SelectionBase]
public class AgentAI : MonoBehaviour
{
    public      AgentState          state           = AgentState.Idle;
    public      AgentMovementDir    movementDir     = AgentMovementDir.None;

    //STATES
    public StateMachine fsm;

    [HideInInspector] public AILocomotion locomotionState;
    [HideInInspector] public AIDispatching dispatchingState;
    [HideInInspector] public AIRepositioning repositioningState;
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
    [HideInInspector] public int dispatchHash;
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
        SetFiniteStateMachine();
    }

    private void Start()
    {
        target = CombatDirector.DistanceInfo.Target;
        CheckCurrentLine();
        destinationLine = 1;
        fsm.State = locomotionState;
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
        dispatchHash = Animator.StringToHash("Dispatch");
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

        locomotionState = GetComponent<AILocomotion>();
        dispatchingState = GetComponent<AIDispatching>();
        repositioningState = GetComponent<AIRepositioning>();
        attackingState = GetComponent<AIAttacking>();
        retreatState = GetComponent<AIRetrat>(); //retreating?
        stunState = GetComponent<AIOnStun>(); //retreating?
        hitState = GetComponent<AIOnHit>(); //retreating?
        recoverState = GetComponent<AIRecover>(); //recovering?        
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

    public void IsValidLine()
    {
        if (currentLine > CombatDirector.DistanceInfo.Lines) //TODO: funzione dispatching verso ultima linea!
        {
            Repostion();
        }
    }

    public void Repostion()
    {
        RunForward(attackRange);
        fsm.State = repositioningState;
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
                return true;
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
                //Repostion();
            }
        }
        return false;
    }
    private RaycastHit? CheckTowardsDir(Vector3 direction) //RIPETUTA! DRY!
    {
        Vector3 raycastOrigin = transform.position + Vector3.up;
        RaycastHit hitInfo;
        if (Physics.SphereCast(raycastOrigin, agentNM.radius, direction, out hitInfo, CombatDirector.DistanceInfo.LastLineRadius, agentLineOfSightLM))
        {
            return hitInfo;
        }
        return null;
    }

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

    #region MOVEMENT TRIGGER

    public void PullBack(int _destinationLine)
    {
        destinationLine = _destinationLine;
        fsm.State = retreatState;
    }

    public void RunForward(float _stoppingDistance) //Stop all current States.
    {
        movementDir = AgentMovementDir.Forward;
        animator.SetTrigger(dispatchHash);
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
