using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UserInput : MonoBehaviour
{
    public Vector2 moveDir, lookDir;
    public bool westButton;
    public bool northButton;

    public void OnMove(InputAction.CallbackContext context)
    {
        moveDir = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookDir = context.ReadValue<Vector2>();
    }

    public void OnStrike(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            //case InputActionPhase.Disabled:
            //    break;
            //case InputActionPhase.Waiting:
            //    break;
            case InputActionPhase.Started:
                westButton = true;
                break;
            //case InputActionPhase.Performed:
            //    break;
            //case InputActionPhase.Canceled:
            //    break;
            //default:
            //    break;
        }
        //westButton = context.ReadValueAsButton();
    }

    public void OnCounter(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            //case InputActionPhase.Disabled:
            //    break;
            //case InputActionPhase.Waiting:
            //    break;
            case InputActionPhase.Started:
                northButton = true;
                break;
                //case InputActionPhase.Performed:
                //    break;
                //case InputActionPhase.Canceled:
                //    break;
                //default:
                //    break;
        }
    }
}
