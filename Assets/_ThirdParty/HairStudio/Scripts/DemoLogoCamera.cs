using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HairStudio {
    public class DemoLogoCamera : MonoBehaviour
    {
        public float yawSpeed = 1, pitchSpeed = 1;
        public float yaw1, yaw2, pitch1, pitch2;
        public GameObject camHolder;

        void Update() {
            var rate = Mathf.Sin(Time.time * yawSpeed + 90);
            rate = rate / 2 + 0.5f;
            var yaw = Mathf.Lerp(yaw1, yaw2, rate);

            rate = Mathf.Sin(Time.time * pitchSpeed);
            rate = rate / 2 + 0.5f;
            var pitch = Mathf.Lerp(pitch1, pitch2, rate);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
        }
    }
}