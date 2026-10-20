using UnityEngine;

namespace SpiderManRebuild
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        void Awake()
        {
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }
}
