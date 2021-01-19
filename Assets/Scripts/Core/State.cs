using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public abstract class State : MonoBehaviour
{
    protected GameObject go;
    protected StateMachine fsm;

    public State (GameObject _gameObject, StateMachine _stateMachine) //Posso sostituire Game Object con MonoBehavior?
    {
        go = _gameObject;
        fsm = _stateMachine;
    }    
    
    public virtual void OnEnter() { }
    public virtual void OnUpdate() { }
    public virtual void OnFixedUpdate() { }
    public virtual void OnLateUpdate() { }
    public virtual void OnAnimatorUpdate() { }
    public virtual void OnAnimatorIKUpdate() { }
    public virtual void OnExit() { }
}
