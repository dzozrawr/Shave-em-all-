using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HairStudio
{
    public class HairDressingToolkit
    {
        private const string TOOL_RADIUS_PREF_LABEL = "HairDressingToolkit_toolRadius";
        private const string TOOL_STRENGTH_PREF_LABEL = "HairDressingToolkit_toolStrength";
        private const string ROOT_ZONE_PREF_LABEL = "HairDressingToolkit_rootZone";
        private const float MINIMUM_LAST_LENGTH_RATIO = 0.1f;
        private const float MINIMUM_TOOL_STRENGTH = 0.01f;

        private HairDressing dressing;

        public float toolRadius {
            get => EditorPrefs.GetFloat(TOOL_RADIUS_PREF_LABEL);
            set => EditorPrefs.SetFloat(TOOL_RADIUS_PREF_LABEL, value);
        }

        public float toolStrength {
            get => EditorPrefs.GetFloat(TOOL_STRENGTH_PREF_LABEL);
            set => EditorPrefs.SetFloat(TOOL_STRENGTH_PREF_LABEL, value);
        }

        public int rootZone {
            get {
                var val = EditorPrefs.GetInt(ROOT_ZONE_PREF_LABEL);
                return val;
            }
            set => EditorPrefs.SetInt(ROOT_ZONE_PREF_LABEL, value);
        }

        public readonly List<Guide> selection = new List<Guide>();
        public Guide hovered = null;

        public Vector2 toolPosition;
        public HairTool tool = HairTool.Comb;

        public HairDressingToolkit(HairDressing dressing) {
            this.dressing = dressing;
        }

        public void OnMouseMove(Event e) {
            switch (tool) {
                case HairTool.Comb:
                case HairTool.Length:
                case HairTool.ChangeRoots:
                    if (e.control) {
                        toolRadius += e.delta.x;
                        toolRadius = Mathf.Clamp(toolRadius, 10, 200);
                        toolStrength -= e.delta.y / 200;
                        toolStrength = Mathf.Clamp(toolStrength, MINIMUM_TOOL_STRENGTH, 1);
                    } else {
                        toolPosition = e.mousePosition;
                    }
                    break;
                case HairTool.AddGuide:
                    break;
                case HairTool.Select:
                    SetHovered(e);
                    break;
            }
        }

        public void OnClic(Event e) {
            toolPosition = e.mousePosition;
            switch (tool) {
                case HairTool.ChangeRoots: ChangeRoots(!e.shift); break;
                case HairTool.AddGuide: AddGuide(); break;
                case HairTool.Select: Select(); break;
            }
        }

        public void OnDrag(Event e) {
            toolPosition = e.mousePosition;
            switch (tool) {
                case HairTool.ChangeRoots: ChangeRoots(!e.shift); break;
                case HairTool.Comb:
                    var cam = SceneView.currentDrawingSceneView.camera;
                    var screenMotion = e.delta;
                    var motion = cam.transform.rotation * new Vector3(
                        screenMotion.x,
                        -screenMotion.y,
                        0);

                    foreach (var guide in selection) {
                        CombGuide(guide, motion * toolStrength * 0.001f);
                    }
                    break;
                case HairTool.Length:
                    foreach (var guide in selection) {
                        ChangeLength(guide, e.shift);
                    }
                    break;
                case HairTool.Select:
                    SetHovered(e);
                    if (hovered != null && !selection.Contains(hovered)) {
                        selection.Add(hovered);
                    }
                    break;
            }
        }

        private void SetHovered(Event e) {
            float minSqrDistance = 20 * 20;
            hovered = null;
            foreach (var guide in dressing.guides) {
                foreach (var seg in guide.segments) {
                    if (seg == guide.segments.Last()) continue;

                    var wSeg = locToW(seg.localPosition);
                    var wNext = locToW(seg.next.localPosition);
                    var segOnScreen = HandleUtility.WorldToGUIPoint(wSeg);
                    var nextOnScreen = HandleUtility.WorldToGUIPoint(wNext);
                    Vector2 closestOnScreen = GetClosestToSegment(segOnScreen, nextOnScreen, e.mousePosition);
                    var sqrDist = (closestOnScreen - e.mousePosition).sqrMagnitude;
                    if (sqrDist < minSqrDistance) {
                        // check if the point is visible
                        var t = Vector3Utils.InverseLerp(segOnScreen, nextOnScreen, closestOnScreen);
                        if (IsBehindCollider(Vector3.Lerp(wSeg, wNext, t))) continue;

                        minSqrDistance = sqrDist;
                        hovered = guide;
                    }
                }
            }
        }

        private void AddGuide() {
            var ray = HandleUtility.GUIPointToWorldRay(toolPosition);
            RaycastHit hit;
            if (dressing.scalpCollider.Raycast(ray, out hit, 1000)) {
                dressing.guides.Add(GenerateGuide(dressing.transform.InverseTransformPoint(hit.point)));
            }
        }

        private void Select() {
            if (hovered != null) {
                if (selection.Contains(hovered)) {
                    selection.Remove(hovered);
                } else {
                    selection.Add(hovered);
                }
            }
        }

        public void CombGuide(Guide guide, Vector3 motion) {
            bool guideChanged = false;
            foreach (var seg in guide.segments) {
                if (!seg.canMove) continue;

                var influence = GetToolInfluence(seg);
                if (influence == 0) continue;

                guideChanged = true;
                seg.localPosition += dressing.transform.InverseTransformDirection(motion) * influence;
            }
            if (guideChanged) {
                // manage length
                foreach (var s in guide.segments) {
                    if (s.next == null) continue;
                    var localLength = s.next.next == null ? guide.lastSegmentLength : guide.segmentLength;
                    var toNext = s.next.localPosition - s.localPosition;
                    toNext -= toNext.normalized * localLength / dressing.scaleFactor;
                    s.next.localPosition -= toNext;
                }
                // manage collisions
                float rate = 0;
                float rateStep = 1.0f / (guide.segments.Count - 1);
                foreach (var s in guide.segments) {
                    if (!s.canMove) continue;
                    foreach (var col in dressing.colliderInfos) {
                        s.localPosition += dressing.transform.InverseTransformDirection(col.GetDepenetration(locToW(s.localPosition), Mathf.Lerp(dressing.scalpSpacing, dressing.scalpSpacingAtTip, rate)));
                    }
                    rate += rateStep;
                }
                if (Application.isPlaying) {
                    dressing.dirtyGuides.Add(guide);
                }
            }
        }

        public void ChangeLength(Guide guide, bool grow) {
            if (grow) {
                var last = guide.segments.Last();
                var influence = GetToolInfluence(last);
                if (influence <= 0) return;

                var direction = (last.localPosition - last.previous.localPosition).normalized;
                guide.lastSegmentLength += influence * toolStrength * 0.01f;
                while (guide.lastSegmentLength > guide.segmentLength * (1 + MINIMUM_LAST_LENGTH_RATIO)) {
                    // we add a segment
                    guide.lastSegmentLength -= guide.segmentLength;
                    last.localPosition = last.previous.localPosition + direction * guide.segmentLength / dressing.scaleFactor;
                    var newSeg = new GuideSegment();
                    newSeg.canMove = true;
                    newSeg.previous = last;
                    last.next = newSeg;
                    guide.segments.Add(newSeg);
                    last = newSeg;
                }
                last.localPosition = last.previous.localPosition + direction * guide.lastSegmentLength;
            } else {
                // cut
                var influence = guide.segments.Max(s => GetToolInfluence(s));
                if (influence <= 0) return;

                var last = guide.segments.Last();
                guide.lastSegmentLength -= influence * toolStrength * 0.01f;

                // we limit the cut if the hair has only one segment
                if (guide.segments.Count == 2) {
                    guide.lastSegmentLength = Mathf.Max(guide.lastSegmentLength, dressing.minimumHairLength);
                }
                if (guide.lastSegmentLength <= guide.segmentLength * MINIMUM_LAST_LENGTH_RATIO) {
                    // we remove the last segment
                    guide.lastSegmentLength += guide.segmentLength;
                    guide.segments.Remove(guide.segments.Last());

                    last = guide.segments.Last();
                    last.next = null;
                }
                last.localPosition = last.previous.localPosition + (last.localPosition - last.previous.localPosition).normalized * guide.lastSegmentLength;
            }
        }

        public void ChangeRoots(bool add) {
            if (dressing.scalpCollider == null) throw new System.Exception("Scalp collider missing. A mesh collider is required for the scalp to paint the roots on.");

            if (add) {
                // we first locate the other roots in the zone
                // these roots must be set as concurrent to avoid placing a new root close to an existing one
                // these roots will also change zone.
                List<Vector3> concurrents = new List<Vector3>();
                foreach (var root in dressing.roots.Get().ToList()) {
                    var wRoot = locToW(root.LocalPos);
                    if (IsBehindCollider(wRoot)) continue;
                    var toolToRoot = HandleUtility.WorldToGUIPoint(wRoot) - toolPosition;

                    if (toolToRoot.sqrMagnitude < MathUtility.Square(toolRadius + dressing.rootRadius)) {
                        // root may collide, we set it as a concurrent
                        concurrents.Add(wRoot);
                        if (root.Zone != rootZone && toolToRoot.sqrMagnitude < MathUtility.Square(toolRadius)) {
                            // root is in tool range, we change its zone if its different
                            dressing.roots.Remove(root);
                            dressing.roots.Add(new Root(rootZone, root.LocalPos));
                        }
                    }
                }
                // we don't add any root if the tool strength is at minimum, so minimum strength can be used to only change zone.
                if (toolStrength <= MINIMUM_TOOL_STRENGTH) return;

                for (int i = 0; i < 10 * toolStrength; i++) {
                    var randomPos = toolPosition + RandomUtility.InsideUnitCircleUniform() * toolRadius;
                    var ray = HandleUtility.GUIPointToWorldRay(randomPos);
                    RaycastHit hit;
                    if (dressing.scalpCollider.Raycast(ray, out hit, 1000)) {
                        // the ray has hit the scalp collider
                        Vector3 candidate = hit.point;
                        // discard cadidate if a concurrent is too close
                        if (concurrents.Any(concurrent => (concurrent - candidate).sqrMagnitude < MathUtility.Square(dressing.rootRadius))) continue;
                        concurrents.Add(candidate);
                        dressing.roots.Add(new Root(rootZone, dressing.transform.InverseTransformPoint(candidate)));
                    }
                }
            } else {
                float toRemoveCount = 10 * toolStrength;
                foreach (var root in dressing.roots.Get().ToList()) {
                    var wRoot = locToW(root.LocalPos);

                    var toolToRoot = HandleUtility.WorldToGUIPoint(wRoot) - toolPosition;
                    if (toolToRoot.sqrMagnitude >= toolRadius * toolRadius) continue;

                    if (IsBehindCollider(wRoot)) {
                        continue;
                    }

                    dressing.roots.Remove(root);
                    if (--toRemoveCount <= 0) {
                        return;
                    }
                }
            }
        }

        public void GenerateGuides(int guideCount, float guideDensity) {
            dressing.guides.Clear();
            int count = guideCount != 0 ? guideCount : (int)(dressing.roots.Get().Count * guideDensity);
            var shuffledLocalRoots = dressing.roots.Get().Shuffle().ToList();
            for (int i = 0; i < count; i++) {
                var root = shuffledLocalRoots[i];
                dressing.guides.Add(GenerateGuide(root.LocalPos));
            }
        }

        private float GetToolInfluence(GuideSegment segment) {
            var toolToSegment = HandleUtility.WorldToGUIPoint(locToW(segment.localPosition)) - toolPosition;

            // 0 if the tool is too far or behind a collider
            if (toolToSegment.sqrMagnitude > toolRadius * toolRadius) return 0;
            if (IsBehindCollider(locToW(segment.localPosition))) return 0;

            return (toolRadius - toolToSegment.magnitude) / toolRadius;
        }

        private Guide GenerateGuide(Vector3 localRoot) {
            var guide = new Guide();
            guide.localRotation = Quaternion.LookRotation(localRoot);
            guide.segmentLength = guide.lastSegmentLength = dressing.segmentLength;
            guide.mixedLock = false;

            GuideSegment previous = null;
            for (int j = 0; j < dressing.segmentCountPerGuide; j++) {
                var seg = new GuideSegment();
                if (previous == null) {
                    seg.canMove = false;
                    seg.localPosition = guide.localRotation * Vector3.forward * localRoot.magnitude;
                } else {
                    seg.canMove = true;
                    var offset = guide.localRotation * Vector3.forward * dressing.segmentLength;
                    offset /= dressing.scaleFactor;
                    seg.localPosition = previous.localPosition + offset;

                    // link
                    previous.next = seg;
                    seg.previous = previous;
                }
                previous = seg;
                guide.segments.Add(seg);
            }
            selection.Add(guide);
            return guide;
        }

        private Vector3 locToW(Vector3 v) {
            return dressing.transform.TransformPoint(v);
        }

        private bool IsBehindCollider(Vector3 pos) {
            var toCam = SceneView.lastActiveSceneView.camera.transform.position - pos;
            var scalpToPos = pos - dressing.transform.position;
            return Physics.Raycast(pos + scalpToPos * 0.01f, toCam, toCam.magnitude);
        }

        private Vector3 GetClosestToSegment(Vector3 a, Vector3 b, Vector3 point) {
            var proj = a + Vector3.Project(point - a, b - a);
            var projToA = (a - proj).sqrMagnitude;
            var projToB = (b - proj).sqrMagnitude;
            var AToB = (b - a).sqrMagnitude;

            if (projToA > AToB) {
                return projToA > projToB ? b : a;
            } else if (projToB > AToB) {
                return projToB > projToA ? a : b;
            }
            return proj;
        }
    }
}
