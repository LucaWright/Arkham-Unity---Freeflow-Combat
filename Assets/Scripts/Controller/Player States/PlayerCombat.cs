using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : State
{
    public PlayerCombat(GameObject go, StateMachine fsm) : base(go, fsm) { }

    Player player;

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
        //player.state = PlayerState.Counter;
        StartCoroutine(ExitCombatState());
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        //HANDLE INPUT
        //player.SetMovementVector();
        //if (player.CheckCombatInput())
        //{
        //    //
        //}
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        if (player.input.westButton)
        {
            fsm.State = player.combatStrike;
        }

        if (player.input.northButton)
        {
            fsm.State = player.combatCounter;
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        StopAllCoroutines();
    }

    public IEnumerator ImpactTimeFreeze()
    {
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(.04f);
        Time.timeScale = 1;
    }

    IEnumerator ExitCombatState()
    {
        yield return new WaitForSeconds(.5f);
        fsm.State = player.locomotionState;
    }
}
