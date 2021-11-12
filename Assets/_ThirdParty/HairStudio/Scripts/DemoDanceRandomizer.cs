using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HairStudio {
    public class DemoDanceRandomizer : MonoBehaviour
    {
        private float offset, duration, currentDuration;

        Animator animator { get => GetComponent<Animator>(); }

        public AnimationCurve curve;
        public float amplitude = 1;
        public Motion motion;

        private void Start() {
            offset = Random.Range(0f, 1f);
            duration = motion.averageDuration;
        }

        void Update() {
            currentDuration += Time.deltaTime;
            if(currentDuration > duration) {
                currentDuration -= duration;
            }
            var rate = currentDuration / duration;
            rate += offset;
            if (rate > 1) rate -= 1;
            var speed = ((curve.Evaluate(rate) - 1) * amplitude) + 1;
            animator.speed = speed;
        }
    }
}