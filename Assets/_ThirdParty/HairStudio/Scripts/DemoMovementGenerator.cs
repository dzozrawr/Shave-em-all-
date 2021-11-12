using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HairStudio {
    public class DemoMovementGenerator : MonoBehaviour
    {
        public List<Movement> movements;

        void Update() {
            movements.ForEach(m => m.Apply(gameObject));
        }
    }

    [Serializable]
    public class Movement
    {
        public enum Axis
        {
            X, Y, Z, Yaw, Pitch, Roll
        }
        public float speed, startValue, endValue;
        public Axis axis;

        public void Apply(GameObject go) {
            var rate = Mathf.Sin(Time.time* speed);
            rate = rate / 2 + 0.5f;
            var value = Mathf.Lerp(startValue, endValue, rate);
            go.transform.localPosition = new Vector3(
                axis == Axis.X ? value : go.transform.localPosition.x,
                axis == Axis.Y ? value : go.transform.localPosition.y,
                axis == Axis.Z ? value : go.transform.localPosition.z);
            go.transform.localRotation = Quaternion.Euler(
                axis == Axis.Pitch ? value : go.transform.localEulerAngles.x,
                axis == Axis.Yaw ? value : go.transform.localEulerAngles.y,
                axis == Axis.Roll ? value : go.transform.localEulerAngles.z);
        }
    }
}