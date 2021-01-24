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
    public GameObject go;

    Player player = null;
    PlayerCombat combatState = null;
    CapsuleCollider capsuleCollider = null;
    public Transform transform = null;


    public float strikeImpulseForceMagnitude = 10f;
    Vector3 strikeImpulseForce;

    public AgentAI target;

    public float fow = 45f;

    public float strikeSpeed = 3f;
    public float strikeBeat = .75f;
    public Ease strikeEase;
    public LayerMask strikeLayerMask;
    public float stoppingDistance = .75f;

    public GameObject hitPointRef;
    public UnityEvent OnStrikeStartFX;
    public UnityEvent OnStrikeImpactFX;
    public UnityEvent OnStrikeEndFX;

    private void Awake()
    {
        //player = go.GetComponent<Player>();
        //combatState = go.GetComponent<PlayerCombat>();
        //capsuleCollider = go.GetComponent<CapsuleCollider>();
    }

    public void SetAction(Transform playerTransform)
    {
        transform = playerTransform;
    }

    public IEnumerator Anticipation() //vuole dei parametri in entrata. Ergo, transform suo, transform del target e animator. Su target dovrebbe addirittura fare get component. Non so, credo sia da abbandonare questa via.
    {
        player.animator.SetTrigger("Strike");
        OnStrikeStartFX.Invoke();

        float easedValue = 0;
        float attackPercentage = 0;

        Vector3 startingAttackPosition = player.transform.position;
        Vector3 vecToTarget = target.transform.position - player.transform.position;
        Vector3 separator = -vecToTarget.normalized * stoppingDistance;

        //IDEA: Gestire ritmo\distanza tramite animation curve!

        //strikeImpulseForce = player.mass * (vecToTarget + separator) / (strikeBeat /** Time.fixedDeltaTime*/); //andrebbe puntata un po' in basso
        strikeImpulseForce = (vecToTarget + separator) * strikeImpulseForceMagnitude;

        //Handle rotation
        player.transform.rotation = Quaternion.LookRotation(vecToTarget, Vector3.up);

        while (attackPercentage / strikeBeat < 1)
        {
            attackPercentage += Time.fixedDeltaTime;
            easedValue = DOVirtual.EasedValue(0, 1, Mathf.Clamp01(attackPercentage / strikeBeat), strikeEase);
            player.transform.position = Vector3.Lerp(startingAttackPosition, target.transform.position + separator, easedValue);
            yield return new WaitForFixedUpdate();
        }
    }

    public IEnumerator Execution()
    {
        yield break;
    }

    public IEnumerator Impact()
    {
        yield break;
    }

    public IEnumerator Recovery()
    {
        yield break;
    }
}
