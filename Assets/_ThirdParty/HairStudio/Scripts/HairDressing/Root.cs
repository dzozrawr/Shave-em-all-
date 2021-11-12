using System;
using UnityEngine;

namespace HairStudio
{
    [Serializable]
    public class Root
    {
        public const int A = 0, B = 1, C = 2, AB = 3, BC = 4, CA = 5;
        [SerializeField, HideInInspector]
        private int zone;
        [SerializeField, HideInInspector]
        private Vector3 localPos;

        public int Zone => zone;
        public Vector3 LocalPos => localPos;

        public Root(int zone, Vector3 localPos) {
            this.zone = zone;
            this.localPos = localPos;
        }
    }
}