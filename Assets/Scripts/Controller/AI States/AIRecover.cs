﻿using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

public class AIRecover : State
{
    public AIRecover(GameObject go, StateMachine fsm) : base(go, fsm) { }

    AgentAI agentAI;
    public float reanimationBlendTime = .5f;

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
        shotHeadPos = agentAI.animator.GetBoneTransform(HumanBodyBones.Head).position;
        shotFeetPos = (agentAI.animator.GetBoneTransform(HumanBodyBones.RightFoot).position + agentAI.animator.GetBoneTransform(HumanBodyBones.LeftFoot).position) * .5f;

        agentAI.animator.SetTrigger("Getting Up");
        EvaluateForward();
        //Debug.Break();
        agentAI.animator.enabled = true;

        yield return new WaitForFixedUpdate();

        Vector3 newRootPosition = ResetHipPosition();

        RaycastHit[] hits = Physics.RaycastAll(new Ray(newRootPosition, Vector3.down));
        newRootPosition.y = float.MinValue;
        foreach (RaycastHit hit in hits)
        {
            if (!hit.transform.IsChildOf(transform))
            {
                newRootPosition.y = Mathf.Max(newRootPosition.y, hit.point.y);
            }
        }
        transform.position = newRootPosition;

        NavMeshHit navMeshHit;
        if (NavMesh.SamplePosition(newRootPosition, out navMeshHit, 2f, NavMesh.AllAreas))
        {
            transform.position = navMeshHit.position;
        }

        Vector3 ragdollDirection = shotHeadPos - shotFeetPos;
        ragdollDirection.y = 0;

        Vector3 animationHeadPos = agentAI.animator.GetBoneTransform(HumanBodyBones.Head).position;
        Vector3 animationFeetPos = (agentAI.animator.GetBoneTransform(HumanBodyBones.RightFoot).position + agentAI.animator.GetBoneTransform(HumanBodyBones.LeftFoot).position) * .5f;
        Vector3 animationDirection = animationHeadPos - animationFeetPos;
        animationDirection.y = 0;

        transform.rotation *= Quaternion.FromToRotation(animationDirection.normalized, ragdollDirection.normalized); //normalizzare serve davvero???

        //yield return new WaitForFixedUpdate();
        agentAI.agentNM.enabled = true;
        agentAI.rootMotion = Vector3.zero; //Questo è meglio che venga fatto in hit?
        //yield return new WaitForFixedUpdate();
        isRootMotionActive = true;

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
        agentAI.avoidanceCollider.enabled = true;
        isRootMotionActive = true;
        StartCoroutine(agentAI.BackToIdle1());
    }

    public void EvaluateForward()
    {
        if (agentAI.rootBone.forward.y >= 0)
        {
            agentAI.animator.SetTrigger("From Back");
            //agentAI.rootBone.rotation.SetLookRotation(Vector3.up);
        }
        else
        {
            agentAI.animator.SetTrigger("From Front");
            //agentAI.rootBone.rotation.SetLookRotation(Vector3.down);
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
        agentAI.animator.ResetTrigger(agentAI.idleHash);
    }


}