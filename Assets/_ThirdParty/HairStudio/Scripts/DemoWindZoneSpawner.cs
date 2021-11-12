using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HairStudio {
    public class DemoWindZoneSpawner : MonoBehaviour
    {
        private float remainingCooldown;

        public WindForHair windForHair;
        public float cooldown, duration, force, turbulence;
        public Mesh spawnedMesh;
        public Material spawnedMaterial;

        void Update() {
            remainingCooldown -= Time.deltaTime;
            if(remainingCooldown <= 0) {
                remainingCooldown = cooldown;

                var go = UOUtility.Create("Spherical wind zone", gameObject);
                var wz = go.AddComponent<WindZone>();
                wz.mode = WindZoneMode.Spherical;
                wz.windMain = force;
                wz.windTurbulence = turbulence;

                var mf = go.AddComponent<MeshFilter>();
                mf.mesh = spawnedMesh;

                var mr = go.AddComponent<MeshRenderer>();
                mr.material = spawnedMaterial;

                windForHair.AddWindZone(wz);
                Destroy(wz, duration);
                Destroy(go, duration * 5);
            }
        }
    }
}