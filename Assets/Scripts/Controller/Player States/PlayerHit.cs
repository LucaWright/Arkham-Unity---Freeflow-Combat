using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHit : State
{
    public PlayerHit(GameObject go, StateMachine fsm) : base(go, fsm) { }

    Player player;

    public float stunTime = .5f;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    void Start()
    {
        go = this.gameObject;
        fsm = player.fsm;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        //animator.ResetTrigger("Strike");
        //animator.ResetTrigger("Strike Ender");
        //animator.SetTrigger("Stun");
        if (player.state == PlayerState.Hit) return;

        StartCoroutine(ExitStun());
        player.state = PlayerState.Hit;
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        player.movementVector = Vector3.zero; //annulla il vettore così o metti in return l'update fin tanto che sei stun
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
    }

    public override void OnExit()
    {
        base.OnExit();
        CombatDirector.state = CombatDirectorState.Planning;
    }

    IEnumerator ExitStun()
    {
        yield return new WaitForSeconds(stunTime);
        fsm.State = player.locomotionState;
    }
}
