using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HairStudio
{
    public static class UOUtility
    {
        public static GameObject Create(string name, GameObject parent, params Type[] components) {
            var res = new GameObject(name, components);
            res.transform.parent = parent.transform;
            res.transform.localPosition = Vector3.zero;
            res.transform.localScale = Vector3.one;
            res.transform.localRotation = Quaternion.identity;
            return res;
        }

        public static GameObject Instantiate(GameObject prefab, Transform parent) {
            var res = UnityEngine.Object.Instantiate(prefab, parent);
            res.transform.localPosition = Vector3.zero;
            res.transform.localRotation = Quaternion.identity;
            res.transform.localScale = Vector3.one;
            return res;
        }

        public static void Destroy(UnityEngine.Object o) {
            if (Application.isPlaying) {
                UnityEngine.Object.Destroy(o);
            } else {
#if UNITY_EDITOR
                EditorApplication.delayCall += () => {
                    UnityEngine.Object.DestroyImmediate(o);
                    //EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                };
#endif
            }
        }

        public static void DestroyChildren(GameObject go) {
            var childList = go.transform.Cast<Transform>().ToList();
            foreach (Transform childTransform in childList) {
                Destroy(childTransform.gameObject);
            }
        }
    }
}
