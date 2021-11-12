using System;
using System.Collections.Generic;
using UnityEngine;

namespace HairStudio
{
    [Serializable]
    public class RootProvider : ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        private List<Root> roots = new List<Root>();

        private Dictionary<int, List<Root>> rootsByZone = new Dictionary<int, List<Root>>();

        public IReadOnlyList<Root> Get(int zone = -1) {
            if (zone == -1) {
                return roots.AsReadOnly();
            } else {
                return GetZone(zone).AsReadOnly();
            }
        }

        public void Add(Root root) {
            roots.Add(root);
            GetZone(root.Zone).Add(root);
        }

        public void Remove(Root root) {
            roots.Remove(root);
            rootsByZone[root.Zone].Remove(root);
        }

        public void RemoveAll() {
            roots.Clear();
            rootsByZone.Clear();
        }

        private List<Root> GetZone(int zone) {
            if (!rootsByZone.ContainsKey(zone)) {
                rootsByZone[zone] = new List<Root>();
            }
            return rootsByZone[zone];
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize() {
            foreach (var root in roots) {
                GetZone(root.Zone).Add(root);
            }
        }
    }
}