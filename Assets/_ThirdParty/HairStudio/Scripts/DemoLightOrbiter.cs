using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HairStudio {
    public class DemoLightOrbiter : MonoBehaviour
    {
        public float speed = 0.01f;

        private Vector3 euler;

        private void Awake() {
            euler = new Vector3(0,
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f));
        }

        void Update() {
            transform.localRotation = Quaternion.Euler(transform.localEulerAngles + euler * speed);
        }
    }
}