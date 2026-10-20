using UnityEngine;

namespace SpiderManRebuild
{
    public sealed class MainMenuBackdrop : MonoBehaviour
    {
        public Transform Hero;
        public RectTransform WebLayer;
        Vector3 heroPosition;
        Quaternion heroRotation;
        void Start() { heroPosition = Hero.position; heroRotation = Hero.rotation; }
        void Update()
        {
            float t = Time.unscaledTime;
            Hero.position = heroPosition + Vector3.up * (Mathf.Sin(t * .55f) * .025f);
            Hero.rotation = heroRotation * Quaternion.Euler(0, Mathf.Sin(t * .22f) * 5, 0);
            if (WebLayer != null) WebLayer.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * .035f) * 1.5f);
        }
    }
}
