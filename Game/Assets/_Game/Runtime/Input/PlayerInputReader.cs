using UnityEngine;
using UnityEngine.InputSystem;

namespace SpiderManRebuild
{
    [DefaultExecutionOrder(-200)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] InputActionAsset controls;
        InputActionAsset instance;
        InputActionMap map;
        InputAction move, look, jump, sprint, pause, reset;
        public PlayerCommand Command { get; private set; }
        public bool Automation { get; set; }

        public void Configure(InputActionAsset asset) => controls = asset;

        void Awake()
        {
            instance = Instantiate(controls);
            map = instance.FindActionMap("Gameplay", true);
            move = map.FindAction("Move", true);
            look = map.FindAction("Look", true);
            jump = map.FindAction("Jump", true);
            sprint = map.FindAction("Sprint", true);
            pause = map.FindAction("Pause", true);
            reset = map.FindAction("Reset", true);
        }

        void OnEnable() { if (map != null) map.Enable(); }
        void Start() => map.Enable();
        void Update()
        {
            if (Automation) return;
            Command = new PlayerCommand {
                Move = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1),
                Look = look.ReadValue<Vector2>(),
                JumpPressed = jump.WasPressedThisFrame(),
                SprintHeld = sprint.IsPressed(),
                PausePressed = pause.WasPressedThisFrame(),
                ResetPressed = reset.WasPressedThisFrame()
            };
        }

        public void Inject(PlayerCommand command) { Automation = true; Command = command; }
        public void Clear() => Command = default;
        void OnDisable() { map?.Disable(); Clear(); }
        void OnDestroy() { if (instance != null) Destroy(instance); }
    }
}
