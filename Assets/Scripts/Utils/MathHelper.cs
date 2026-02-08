using UnityEngine;

namespace NexusPrime.Utils
{
    public static class MathHelper
    {
        public static float Remap(float value, float inMin, float inMax, float outMin, float outMax)
        {
            return outMin + (value - inMin) / (inMax - inMin) * (outMax - outMin);
        }

        public static Vector3 RandomPointInCircle(Vector3 center, float radius)
        {
            var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            return center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
        }

        public static bool Approximately(Vector3 a, Vector3 b, float threshold = 0.01f)
        {
            return Vector3.SqrMagnitude(a - b) < threshold * threshold;
        }
    }
}
