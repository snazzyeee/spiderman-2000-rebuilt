using UnityEngine;
using UnityEngine.InputSystem;

namespace SpiderManRebuild
{
    [DefaultExecutionOrder(-300)]
    public sealed class TraversalInput : MonoBehaviour
    {
        InputActionMap map;
        InputAction move, mouse, stick, jump, run, pause, reset;
        public Vector2 Move => move.ReadValue<Vector2>();
        public Vector2 MouseDelta => mouse.ReadValue<Vector2>();
        public Vector2 LookStick => stick.ReadValue<Vector2>();
        public bool JumpPressed => jump.WasPressedThisFrame();
        public bool RunHeld => run.IsPressed();
        public bool PausePressed => pause.WasPressedThisFrame();
        public bool ResetPressed => reset.WasPressedThisFrame();
        public bool Automation { get; set; }
        public InputActionMap Actions => map;
        void Awake()
        {
            map = new InputActionMap("Traversal");
            move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");
            mouse = map.AddAction("MouseLook", InputActionType.PassThrough, "<Mouse>/delta");
            stick = map.AddAction("StickLook", InputActionType.Value, "<Gamepad>/rightStick");
            jump = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            run = Button("Run", "<Keyboard>/leftShift", "<Gamepad>/leftShoulder");
            pause = Button("Pause", "<Keyboard>/escape", "<Gamepad>/start");
            reset = Button("Reset", "<Keyboard>/r", "<Gamepad>/select");
        }
        InputAction Button(string name, string keyboard, string gamepad)
        {
            var action = map.AddAction(name, InputActionType.Button, keyboard);
            action.AddBinding(gamepad); return action;
        }
        void OnEnable() => map?.Enable();
        void OnDisable() => map?.Disable();
        void OnDestroy() => map?.Dispose();
    }
}
