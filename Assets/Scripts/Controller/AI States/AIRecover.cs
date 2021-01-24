using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

public class AIRecover : State
{
    public AIRecover(GameObject go, StateMachine fsm) : base(go, fsm) { }

    AgentAI agentAI;
    Animator animator;
    NavMeshAgent agentNM;
    SphereCollider avoidanceCollider;

    int idleHash;
    int getUpFrontHash;
    int getUpBackHash;

    public float reanimationBlendTime = .5f;
    public float recoveryAgentNMradius = .25f;

    Vector3 shotHipPos;
    Vector3 shotHeadPos;
    Vector3 shotFeetPos;
    public LayerMask envGeometryLM;

    bool isRootMotionActive = false;

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

        idleHash = agentAI.idleHash;

        SetRecoveringHashParameters();

        //TODO
        //Set trigger string to hash
    }

    void SetRecoveringHashParameters()
    {
        getUpFrontHash = Animator.StringToHash("GetUp Front");
        getUpBackHash = Animator.StringToHash("GetUp Back");
    }

    public override void OnEnter()
    {
        base.OnEnter();
        isRootMotionActive = false;
        agentAI.state = AgentState.Recover;
        StartCoroutine(RecoverExecution());
    }

    public override void OnUpdate()
    {
        base.OnUpdate();        
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        if (isRootMotionActive)
        {
            agentAI.HandleRootMotionMovement();
        }
    }

    //TODO
    //Ridurre a zero (o quasi) il radius del navmesh
    //Scegliere un momento migliore per riattivare il coollider

    IEnumerator RecoverExecution()
    {
        //TAKE SNAPSHOT
        agentAI.SetRagdollKinematicRigidbody(true);
        //snapshot ragdoll position
        foreach (RagdollSnapshot snapshot in agentAI.ragdollSnapshot)
        {
            snapshot.shotPos = snapshot.transform.position;
            snapshot.shotRot = snapshot.transform.rotation;
        }

        shotHipPos = agentAI.rootBone.position;
        shotHeadPos = animator.GetBoneTransform(HumanBodyBones.Head).position;
        shotFeetPos = (animator.GetBoneTransform(HumanBodyBones.RightFoot).position + animator.GetBoneTransform(HumanBodyBones.LeftFoot).position) * .5f;

        EvaluateForward();
        //Debug.Break();
        animator.enabled = true; //Deve in qualche modo stoppare l'animazione in corso prima della transizione?

        yield return new WaitForFixedUpdate();

        Vector3 newRootPosition = ResetHipPosition();

        RaycastHit hitInfo;
        if (Physics.Raycast(shotHipPos, Vector3.down, out hitInfo, 2f, envGeometryLM))
        {
            newRootPosition.y = hitInfo.point.y;
        }
        else
        {
            Debug.Log("Anche il tuo raycast nuovo ha fallito");
        }
        transform.position = newRootPosition;

        NavMeshHit navMeshHit;
        if (NavMesh.SamplePosition(newRootPosition, out navMeshHit, 10f, NavMesh.AllAreas))
        {
            transform.position = navMeshHit.position;
        }
        else
        {
            Debug.Log("Sarà qui l'errore?");
            Debug.Break();
        }

        Vector3 ragdollDirection = shotHeadPos - shotFeetPos;
        ragdollDirection.y = 0;

        Vector3 animationHeadPos = animator.GetBoneTransform(HumanBodyBones.Head).position;
        Vector3 animationFeetPos = (animator.GetBoneTransform(HumanBodyBones.RightFoot).position + animator.GetBoneTransform(HumanBodyBones.LeftFoot).position) * .5f;
        Vector3 animationDirection = animationHeadPos - animationFeetPos;
        animationDirection.y = 0;

        transform.rotation *= Quaternion.FromToRotation(animationDirection.normalized, ragdollDirection.normalized); //normalizzare serve davvero???

        agentNM.enabled = true;
        agentAI.rootMotion = Vector3.zero;
        isRootMotionActive = true;
        agentNM.radius = recoveryAgentNMradius;
        //Debug.Break();

        float blendAmount = 0;

        while (blendAmount < 1)
        {
            blendAmount += Time.fixedDeltaTime / reanimationBlendTime;
            Mathf.Clamp01(blendAmount);

            foreach (RagdollSnapshot snapshot in agentAI.ragdollSnapshot)
            {
                if (snapshot.transform == agentAI.rootBone)
                {
                    snapshot.transform.position = Vector3.Lerp(snapshot.shotPos, snapshot.transform.position, blendAmount);
                }
                snapshot.transform.rotation = Quaternion.Slerp(snapshot.shotRot, snapshot.transform.rotation, blendAmount);
            }
            yield return new WaitForFixedUpdate();
        }


        yield return new WaitForFixedUpdate();
        avoidanceCollider.enabled = true;
        avoidanceCollider.radius = recoveryAgentNMradius;
        isRootMotionActive = true;
        //StartCoroutine(agentAI.BackToIdle());
        //agentAI.BackToIdle();

        while (!animator.IsInTransition(0))
        {
            yield return new WaitForFixedUpdate();
        }
        //yield return new WaitForSeconds(1f);
        agentNM.radius = agentAI.agentNMradius;
        avoidanceCollider.radius = agentAI.agentNMradius;
        fsm.State = agentAI.locomotionState;

    }

    public void EvaluateForward()
    {
        if (agentAI.rootBone.forward.y >= 0)
        {
            animator.SetTrigger(getUpBackHash);
        }
        else
        {
            animator.SetTrigger(getUpFrontHash);
        }
    }

    Vector3 ResetHipPosition()
    {
        Vector3 animToRagdollDistanceVector = shotHipPos - agentAI.rootBone.position;
        return transform.position + animToRagdollDistanceVector;
    }

    public override void OnExit()
    {
        base.OnExit();
        StopAllCoroutines();
        animator.ResetTrigger(idleHash);
    }


}
