using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HairStudio {
    public class DemoDynamicSimulation : MonoBehaviour
    {
        public float speed;

        void Update() {
            var rate = Mathf.Sin(Time.time * speed);
            rate = rate / 2 + 0.5f;
            GetComponent<HairSimulation>().localStiffness = rate * 0.8f;
        }
    }
}