using UnityEngine;

namespace SpiderManRebuild
{
    [DefaultExecutionOrder(-400)]
    public sealed class SpiderManPlayerInstaller : MonoBehaviour
    {
        public Transform ImportedVisual;
        public MovementTuning Movement;
        void Awake()
        {
            if (ImportedVisual == null || ImportedVisual == transform || Movement == null)
            { Debug.LogError("Assign the imported visual child and movement tuning to SpiderManPlayerInstaller.", this); enabled = false; return; }
            if (transform.lossyScale != Vector3.one || Quaternion.Angle(transform.rotation, Quaternion.identity) > .01f)
            { Debug.LogError("The player root must have identity world scale and rotation; scale/rotate only ImportedVisual.", this); enabled = false; return; }
            if (GetComponent<SpiderManCharacterController>() == null)
                SpiderManRigSetup.Install(gameObject, ImportedVisual, Movement);
        }
    }
}
