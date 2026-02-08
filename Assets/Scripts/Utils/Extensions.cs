using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace NexusPrime.Utils
{
    public static class Extensions
    {
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }

        public static void SetLayerRecursively(this GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                child.gameObject.SetLayerRecursively(layer);
        }

        public static List<T> Shuffle<T>(this List<T> list)
        {
            var result = new List<T>(list);
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var t = result[i];
                result[i] = result[j];
                result[j] = t;
            }
            return result;
        }
    }
}
