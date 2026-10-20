using System;
using UnityEngine;

namespace SpiderManRebuild
{
    public sealed class FrameTimeRecorder : MonoBehaviour
    {
        readonly float[] samples = new float[2048];
        readonly float[] sorted = new float[2048];
        int count, cursor;
        public int HitchCount { get; private set; }
        public float P95Milliseconds { get; private set; }
        public float P99Milliseconds { get; private set; }
        public float FramesPerSecond { get; private set; }
        double nextRefresh;
        void Update()
        {
            if (Time.realtimeSinceStartup < 5) return;
            float dt = Time.unscaledDeltaTime;
            samples[cursor] = dt * 1000;
            cursor = (cursor + 1) % samples.Length;
            count = Mathf.Min(count + 1, samples.Length);
            if (dt > 1f / 30) HitchCount++;
            if (Time.realtimeSinceStartupAsDouble < nextRefresh) return;
            nextRefresh = Time.realtimeSinceStartupAsDouble + .5;
            Array.Copy(samples, sorted, count);
            Array.Sort(sorted, 0, count);
            P95Milliseconds = sorted[Mathf.Clamp(Mathf.CeilToInt(count * .95f) - 1, 0, count - 1)];
            P99Milliseconds = sorted[Mathf.Clamp(Mathf.CeilToInt(count * .99f) - 1, 0, count - 1)];
            float total = 0;
            for (int i = 0; i < count; i++) total += samples[i];
            FramesPerSecond = total > 0 ? count * 1000 / total : 0;
        }
    }
}
