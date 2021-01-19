using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Animated Action/Anticipation")]
[Serializable]
public class AnimatedAction : ScriptableObject, IAnimatedAction
{
    /*//PROBLEMI
    //1. Non ha referenza di player. Gliela devo passare io a mano.
    //2. Gli event devono essere nello scriptable, non nel game object!
    //3. Difficile fare tutto da una referenza generica!
    Transform transform;
    Animator animator;

    public UnityEvent OnStrikeStartFX;
    
    public IEnumerator Anticipation()
    {
        //animator.SetTrigger("Strike"); //L'animator può prenderlo per conto suo?
        //OnStrikeStartFX.Invoke();

        //float easedValue = 0;
        //float attackPercentage = 0;

        //Vector3 startingAttackPosition = transform.position;
        //Vector3 vecToTarget = target.transform.position - transform.position;
        //Vector3 separator = -vecToTarget.normalized * stoppingDistance;

        //strikeImpulseForce = (vecToTarget + separator) * strikeImpulseForceMagnitude;

        ////Handle rotation
        //transform.rotation = Quaternion.LookRotation(vecToTarget, Vector3.up);

        //while (attackPercentage / strikeBeat < 1)
        //{
        //    attackPercentage += Time.fixedDeltaTime;
        //    easedValue = DOVirtual.EasedValue(0, 1, Mathf.Clamp01(attackPercentage / strikeBeat), strikeEase);
        //    transform.position = Vector3.Lerp(startingAttackPosition, target.transform.position + separator, easedValue);
        //    yield return new WaitForFixedUpdate();
        //}
    }

    public IEnumerator Execution()
    {
        throw new System.NotImplementedException();
    }

    public IEnumerator Impact()
    {
        ////STRIKE IMPACT
        //player.animator.SetTrigger("Strike Ender");
        //hitPointRef.transform.position = target.chestTransf.position; //compromesso

        //target.OnHit(strikeImpulseForce);
        //OnStrikeImpactFX.Invoke();
        //yield return combatState.ImpactTimeFreeze(); //dovrebbe essere presa sempre dagli event

        //player.input.westButton = false; //farglielo fare all'input manager? 
        //player.input.northButton = false;

        //OnStrikeEndFX.Invoke();

        //fsm.State = player.combatState;
    }

    public IEnumerator Recovery()
    {
        throw new System.NotImplementedException();
    }*/
    public IEnumerator Anticipation()
    {
        throw new NotImplementedException();
    }

    public IEnumerator Execution()
    {
        throw new NotImplementedException();
    }

    public IEnumerator Impact()
    {
        throw new NotImplementedException();
    }

    public IEnumerator Recovery()
    {
        throw new NotImplementedException();
    }
}
