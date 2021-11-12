using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HairStudio
{
    public class WindForHair : MonoBehaviour
    {
        private List<WindZone> sphericalWindzones, directionalWindzones;
        public List<HairSimulation> simulations;
        public float forceMultiplier = 1, turbulenceMultiplier = 1;

        public int sphcount, dircount;

        private void Awake() {
            var windzones = FindObjectsOfType<WindZone>();
            sphericalWindzones = windzones.Where(wz => wz.mode == WindZoneMode.Spherical).ToList();
            directionalWindzones = windzones.Where(wz => wz.mode == WindZoneMode.Directional).ToList();
        }

        public void AddWindZone(WindZone wz) {
            switch (wz.mode) {
                case WindZoneMode.Spherical: sphericalWindzones.Add(wz); break;
                case WindZoneMode.Directional: directionalWindzones.Add(wz); break;
            }
        }

        private void FixedUpdate() {
            for (int i = 0; i < sphericalWindzones.Count; i++) {
                if (sphericalWindzones[i] == null) {
                    sphericalWindzones.RemoveAt(i--);
                }
            }
            for (int i = 0; i < directionalWindzones.Count; i++) {
                if (directionalWindzones[i] == null) {
                    directionalWindzones.RemoveAt(i--);
                }
            }

            sphcount = sphericalWindzones.Count();
            dircount = directionalWindzones.Count();

            Vector3 directionalForce = Vector3.zero;
            foreach(var wz in directionalWindzones) {
                directionalForce += GetWind(wz.transform.rotation, wz.windMain, wz.windTurbulence);
            }

            foreach(var sim in simulations) {
                var localForce = directionalForce;
                foreach(var wz in sphericalWindzones) {
                    var toSim = sim.transform.position - wz.transform.position;
                    var distance = toSim.magnitude;
                    if (distance > wz.radius) continue;

                    localForce += GetWind(Quaternion.LookRotation(toSim), wz.windMain * (wz.radius - distance) / wz.radius, wz.windTurbulence);
                }
                sim.externalForce += localForce;
            }
        }

        private Vector3 GetWind(Quaternion direction, float force, float turbulence) {
            var turbulenceDirection = new Vector3(
                (Mathf.PerlinNoise(Time.time, 0) - 0.5f) * 2,
                (Mathf.PerlinNoise(0, Time.time) - 0.5f) * 2,
                1) * turbulence * turbulenceMultiplier;
            var directionWithTurbulence = direction * turbulenceDirection;
            var baseAmplitude = force * forceMultiplier;
            var amplitude = baseAmplitude + baseAmplitude * turbulence * turbulenceMultiplier * (Mathf.PerlinNoise(Time.time, Time.time) - 0.5f) * 2;

            return directionWithTurbulence.normalized * amplitude;

        }
    }
}
