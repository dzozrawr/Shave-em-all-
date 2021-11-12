using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HairStudio
{
    public static class OnCollection
    {
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source) {
            IList<T> res = source.ToList();
            int n = res.Count;
            while (n > 1) {
                n--;
                int k = Random.Range(0, n + 1);
                T value = res[k];
                res[k] = res[n];
                res[n] = value;
            }
            return res;
        }
    }
}