using UnityEngine;

namespace SpiderManRebuild
{
    [DefaultExecutionOrder(-50)]
    public sealed class RespawnController : MonoBehaviour
    {
        [SerializeField] CharacterMotor motor;
        [SerializeField] ThirdPersonCameraRig cameraRig;
        [SerializeField] PlayerInputReader input;
        [SerializeField] GameSession session;
        [SerializeField] SpawnPoint spawn;
        public int ResetCount { get; private set; }
        public Vector3 SpawnPosition => spawn.transform.position;
        public void Configure(CharacterMotor movement, ThirdPersonCameraRig rig, PlayerInputReader reader,
            GameSession game, SpawnPoint point)
        { motor = movement; cameraRig = rig; input = reader; session = game; spawn = point; }
        void Update()
        {
            if ((!session.IsPaused && input.Command.ResetPressed) || motor.transform.position.y < -16) ResetPlayer();
        }
        public void ResetPlayer()
        {
            Vector3 displacement = spawn.transform.position - motor.transform.position;
            motor.ResetTo(spawn.transform.position, spawn.transform.rotation);
            input.Clear();
            session.ResetCourse();
            cameraRig.ResetHeading(spawn.transform.eulerAngles.y, displacement);
            ResetCount++;
        }
    }
}
