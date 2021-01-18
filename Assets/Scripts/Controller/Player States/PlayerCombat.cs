using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : State
{
    public PlayerCombat(GameObject go, StateMachine fsm) : base(go, fsm) { }

    Player player;
    UserInput input;

    PlayerCombatStrike combatStrike;
    PlayerCombatCounter combatCounter;

    State oldState;

    private void Awake()
    {
        player = GetComponent<Player>();
        combatStrike = GetComponent<PlayerCombatStrike>();
        combatCounter = GetComponent<PlayerCombatCounter>();
    }

    void Start()
    {
        go = this.gameObject;
        fsm = player.fsm;

        input = player.input;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        //TODO
        //Attenzione! Non torna in Locomotion da Strike.
        //Creare una recovery per Strike
        StartCoroutine(ExitCombatState());
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        //TODO
        //Trova un modo migliore rispetto a questa schifezza!
        //Una lista di tutti gli State di Combattimento da cliclare a inizio input per ordinare: STOP ALL COROUTINES!

        //OPPURE
        if (input.westButton)
        {
            //TODO testare se funziona
            oldState?.StopAllCoroutines(); //oldState
            oldState = combatStrike;
            fsm.State = player.combatStrike;
        }

        if (input.northButton)
        {
            oldState?.StopAllCoroutines(); //oldState
            oldState = combatCounter;
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
