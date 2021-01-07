using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine
{
    protected State state;

    public State State
    {
        get => state;

        set
        {
            state?.OnExit();

            state = value;

            value?.OnEnter();
        }
    }
}
