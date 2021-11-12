using UnityEngine;
using UnityEditor;
using System.Linq;

namespace HairStudio
{
    [CustomEditor(typeof(HairSimulation)), CanEditMultipleObjects]
    public class HairSimulationEditor : Editor
    {
        private HairSimulation simulation => (HairSimulation)serializedObject.targetObject;

        private void Awake() {
            var guids = AssetDatabase.FindAssets("HairStudio_Simulation");
            if (!guids.Any()) {
                Debug.LogWarning("Cannot find hair simulation shader. Try reinstalling HairStudio or contact support.");
            } else if (guids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).Distinct().Count() > 1) {
                Debug.LogWarning("An asset in your project uses the same name as the HairStudio simulation shader (or this one is duplicated). Please fix the name collision.");
                foreach (var guid in guids) {
                    Debug.LogWarning("    " + AssetDatabase.GUIDToAssetPath(guid));
                }
            } else {
                simulation.computeShader = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids.First()), typeof(ComputeShader)) as ComputeShader;
            }
        }
    }

    
}
