using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterController : MonoBehaviour
{
    StateMachine stateMachine;

    public string state = "";

    public Vector2 moveDir, lookDir;

    public void OnMove(InputAction.CallbackContext context)
    {
        moveDir = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookDir = context.ReadValue<Vector2>();
    }

    private void Awake()
    {
        stateMachine = new StateMachine();
        //stateMachine.State = new PlayerLocomotionState(gameObject, stateMachine);
        stateMachine.State = GetComponent<PlayerLocomotionState>(); //Creare un dictionary, che leghi un enum ai component? Oppure fare get component ogni volta?

        //Dove si puppa tutte le referenze?
    }

    void Start()
    {
        stateMachine.State.OnEnter();
        state = stateMachine.State.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        stateMachine.State.OnUpdate();
        //Qui dovrebbero essere letti gli input. A prescindere.
        //La manipolazione dell'input viene a seguire
        //Come faccio a far accedere ogni stato a questi input? Devi fare get component del controller?
    }

    private void FixedUpdate()
    {
        stateMachine.State.OnFixedUpdate();
    }

    //private void OnAnimatorMove()
    //{
    //    stateMachine.State.OnAnimatorUpdate();
    //}

    //private void OnAnimatorIK(int layerIndex)
    //{
    //    stateMachine.State.OnAnimatorIKUpdate();
    //}
}