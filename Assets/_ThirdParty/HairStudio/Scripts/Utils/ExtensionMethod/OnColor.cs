using UnityEngine;

namespace HairStudio
{
    public static class OnColor
    {
        public static Color WithAlpha(this Color c, float alpha) {
            c.a = alpha;
            return c;
        }
    }
}