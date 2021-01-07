// GENERATED AUTOMATICALLY FROM 'Assets/Game/PlayerInput.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class @PlayerInput : IInputActionCollection, IDisposable
{
    public InputActionAsset asset { get; }
    public @PlayerInput()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""PlayerInput"",
    ""maps"": [
        {
            ""name"": ""CharacterController_B"",
            ""id"": ""a561b055-93b1-4145-beaa-cb3309eecba9"",
            ""actions"": [
                {
                    ""name"": ""Move"",
                    ""type"": ""Value"",
                    ""id"": ""ad142f9e-4cc2-42f5-b036-e244b787377f"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Look"",
                    ""type"": ""Value"",
                    ""id"": ""686b0393-3e4a-427f-82f1-11eb89f9ef9b"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Strike"",
                    ""type"": ""Button"",
                    ""id"": ""6ac7a28a-3dcf-4c29-b60b-5c78a81270ee"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Counter"",
                    ""type"": ""Button"",
                    ""id"": ""f94fa36b-227d-4371-b6f0-d798cb692c42"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""85d8af99-2cbf-4ccb-b78a-478bfd074478"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": ""StickDeadzone"",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""f333d5cf-323a-4ffc-a09c-59e01b904084"",
                    ""path"": ""<Gamepad>/buttonWest"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Strike"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a6167974-b71c-4fda-8abd-0b30dcd47ef0"",
                    ""path"": ""<Gamepad>/buttonNorth"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Counter"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""3b284d8b-547c-46a0-9cfe-209514351d58"",
                    ""path"": ""<Gamepad>/rightStick"",
                    ""interactions"": """",
                    ""processors"": ""StickDeadzone"",
                    ""groups"": """",
                    ""action"": ""Look"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // CharacterController_B
        m_CharacterController_B = asset.FindActionMap("CharacterController_B", throwIfNotFound: true);
        m_CharacterController_B_Move = m_CharacterController_B.FindAction("Move", throwIfNotFound: true);
        m_CharacterController_B_Look = m_CharacterController_B.FindAction("Look", throwIfNotFound: true);
        m_CharacterController_B_Strike = m_CharacterController_B.FindAction("Strike", throwIfNotFound: true);
        m_CharacterController_B_Counter = m_CharacterController_B.FindAction("Counter", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    // CharacterController_B
    private readonly InputActionMap m_CharacterController_B;
    private ICharacterController_BActions m_CharacterController_BActionsCallbackInterface;
    private readonly InputAction m_CharacterController_B_Move;
    private readonly InputAction m_CharacterController_B_Look;
    private readonly InputAction m_CharacterController_B_Strike;
    private readonly InputAction m_CharacterController_B_Counter;
    public struct CharacterController_BActions
    {
        private @PlayerInput m_Wrapper;
        public CharacterController_BActions(@PlayerInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @Move => m_Wrapper.m_CharacterController_B_Move;
        public InputAction @Look => m_Wrapper.m_CharacterController_B_Look;
        public InputAction @Strike => m_Wrapper.m_CharacterController_B_Strike;
        public InputAction @Counter => m_Wrapper.m_CharacterController_B_Counter;
        public InputActionMap Get() { return m_Wrapper.m_CharacterController_B; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(CharacterController_BActions set) { return set.Get(); }
        public void SetCallbacks(ICharacterController_BActions instance)
        {
            if (m_Wrapper.m_CharacterController_BActionsCallbackInterface != null)
            {
                @Move.started -= m_Wrapper.m_CharacterController_BActionsCallbackInterface.OnMove;
                @Move.performed -= m_Wrapper.m_CharacterController_BActionsCallbackInterface.OnMove;
                @Move.canceled -= m_Wrapper.m_CharacterController_BActionsCallbackInterface.OnMove;
                @Look.started -= m_Wrapper.m_CharacterController_BActionsCallbackInterface.OnLook;
                @Look.performed -= m_Wrapper.m_CharacterController_BActionsCallbackInterface.OnLook;
                @Look.canceled -= m_Wrapper.m_CharacterController_BActionsCallbackInterface.OnLook;
                @Strike.started -= m_Wrapper.m_CharacterController_BActionsCallbackInterface.OnStrike;
                @Strike.performed -= m_Wrapper.m_CharacterController_BActionsCallbackInterface.OnStrike;
                @Strike.canceled -= m_Wrapper.m_CharacterController_BActionsCallbackInterface.OnStrike;
                @Counter.started -= m_Wrapper.m_CharacterController_BActionsCallbackInterface.OnCounter;
                @Counter.performed -= m_Wrapper.m_CharacterController_BActionsCallbackInterface.OnCounter;
                @Counter.canceled -= m_Wrapper.m_CharacterController_BActionsCallbackInterface.OnCounter;
            }
            m_Wrapper.m_CharacterController_BActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Move.started += instance.OnMove;
                @Move.performed += instance.OnMove;
                @Move.canceled += instance.OnMove;
                @Look.started += instance.OnLook;
                @Look.performed += instance.OnLook;
                @Look.canceled += instance.OnLook;
                @Strike.started += instance.OnStrike;
                @Strike.performed += instance.OnStrike;
                @Strike.canceled += instance.OnStrike;
                @Counter.started += instance.OnCounter;
                @Counter.performed += instance.OnCounter;
                @Counter.canceled += instance.OnCounter;
            }
        }
    }
    public CharacterController_BActions @CharacterController_B => new CharacterController_BActions(this);
    public interface ICharacterController_BActions
    {
        void OnMove(InputAction.CallbackContext context);
        void OnLook(InputAction.CallbackContext context);
        void OnStrike(InputAction.CallbackContext context);
        void OnCounter(InputAction.CallbackContext context);
    }
}
