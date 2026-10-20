using UnityEngine;

namespace SpiderManRebuild
{
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] CharacterMotor motor;
        [SerializeField] ThirdPersonCameraRig cameraRig;
        [SerializeField] GameSession session;
        [SerializeField] Transform visual;
        public CharacterMotor Motor => motor;
        public void Configure(PlayerInputReader reader, CharacterMotor movement,
            ThirdPersonCameraRig rig, GameSession game, Transform body)
        { input = reader; motor = movement; cameraRig = rig; session = game; visual = body; }

        void Update()
        {
            if (session.IsPaused) { motor.ClearBufferedActions(); return; }
            PlayerCommand command = input.Command;
            Vector3 direction = cameraRig.YawRotation * new Vector3(command.Move.x, 0, command.Move.y);
            motor.Step(direction, command.SprintHeld, command.JumpPressed, Time.timeAsDouble, Time.deltaTime);
            if (direction.sqrMagnitude > .01f && visual != null)
            {
                Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
                visual.rotation = Quaternion.Slerp(visual.rotation, target, 1 - Mathf.Exp(-14 * Time.deltaTime));
            }
        }
    }
}
