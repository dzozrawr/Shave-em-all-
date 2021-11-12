using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HairStudio {
    public class DemoCameraController : MonoBehaviour
    {
        public Camera cam;
        public Vector2 mouseSensivity = Vector2.one;
        public float speed = 1;

        void Update() {
            var pitch = Input.GetAxis("Mouse Y") * mouseSensivity.y;
            var yaw = Input.GetAxis("Mouse X") * mouseSensivity.x;
            var userRotation = Quaternion.Euler(-pitch, yaw, 0);

            var camLookAt = cam.transform.rotation * userRotation * Vector3.forward;

            var bodyNormal = transform.up;
            var bodyLookAt = Vector3.ProjectOnPlane(camLookAt, bodyNormal);
            transform.rotation = Quaternion.LookRotation(bodyLookAt, transform.up);


            cam.transform.rotation = Quaternion.LookRotation(camLookAt, transform.up);

            var forward = Input.GetAxis("Vertical");
            var strafe = Input.GetAxis("Horizontal");

            transform.position += transform.rotation * new Vector3(strafe, 0, forward) * speed;
        }
    }
}