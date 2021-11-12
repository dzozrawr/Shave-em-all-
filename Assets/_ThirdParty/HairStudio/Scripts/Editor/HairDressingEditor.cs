using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HairStudio
{
    [CustomEditor(typeof(HairDressing))]
    public class HairDressingEditor : Editor
    {
        private const string TAB_PREF_LABEL = "HairDressingEditor_tab";
        private const string CLUMPING_EXPAND_LABEL = "HairDressingEditor_clumpingExpand";
        private const string WAVINESS_EXPAND_LABEL = "HairDressingEditor_wavinessExpand";
        private Texture hairIcon, rootIcon, combIcon, scissorIcon, addIcon, selectIcon, selectAllIcon, selectNoneIcon, deleteIcon, interpolationOnIcon, interpolationOffIcon;
        private GUIStyle tabPageStyle;

        private int tab {
            get => EditorPrefs.GetInt(TAB_PREF_LABEL);
            set => EditorPrefs.SetInt(TAB_PREF_LABEL, value);
        }

        private bool clumpingExpanded {
            get => EditorPrefs.GetBool(CLUMPING_EXPAND_LABEL);
            set => EditorPrefs.SetBool(CLUMPING_EXPAND_LABEL, value);
        }

        private bool wavinessExpanded {
            get => EditorPrefs.GetBool(WAVINESS_EXPAND_LABEL);
            set => EditorPrefs.SetBool(WAVINESS_EXPAND_LABEL, value);
        }

        private int toolIndex = 0;

        private HairDressingToolkit toolkit;
        private HairDressing dressing => (HairDressing)serializedObject.targetObject;

        private float guideDensity = 0.1f;
        private int guideCount = 0;
        private bool guideGeneratorFoldout = false;
        private int rootZoneIndex = 0;

        private void OnEnable() {
            hairIcon = GetIcon("HS_HairIcon");
            rootIcon = GetIcon("HS_RootIcon");

            combIcon = GetIcon("HS_CombIcon");
            scissorIcon = GetIcon("HS_ScissorIcon");
            addIcon = GetIcon("HS_AddIcon");
            selectIcon = GetIcon("HS_SelectIcon");

            selectAllIcon = GetIcon("HS_SelectAllIcon");
            selectNoneIcon = GetIcon("HS_SelectNoneIcon");
            deleteIcon = GetIcon("HS_DeleteIcon");
            interpolationOnIcon = GetIcon("HS_InterpolationOnIcon");
            interpolationOffIcon = GetIcon("HS_InterpolationOffIcon");

            toolkit = new HairDressingToolkit(dressing);
            toolkit.tool = tab == 0 ? HairTool.Comb : HairTool.ChangeRoots;

            toolkit.selection.Clear();
            toolkit.selection.AddRange(dressing.guides);
            toolIndex = 0;
        }

        private Texture2D GetIcon(string name) {
            var guids = AssetDatabase.FindAssets(name);
            if(!guids.Any()) {
                Debug.LogWarning("Cannot find icon for HairDressing inspector : " + name + ". Try reinstalling the asset or contact support.");
                return default;
            } else if (guids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).Distinct().Count() > 1) {
                Debug.LogWarning("An asset in your project uses the same name as an icon for HairStudio (or this one is duplicated). Please fix the name collision. Name was " + name);
                foreach(var guid in guids) {
                    Debug.LogWarning("    " + AssetDatabase.GUIDToAssetPath(guid));
                }
                return default;
            }
            return AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids.First()), typeof(Texture2D)) as Texture2D;
        }

        private void OnSceneGUI() {
            // disable game object transform gyzmo
            if (Selection.activeGameObject == dressing.gameObject) {
                Tools.current = Tool.None;
            }

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            Event e = Event.current;
            switch (e.type) {
                case EventType.MouseMove: OnMouseMove(e); break;
                case EventType.Repaint: OnRepaint(e); break;
                case EventType.MouseDown: OnMouseDown(e); break;
                case EventType.MouseDrag: OnMouseDrag(e); break;
                case EventType.DragUpdated: OnMouseDrag(e); break;
                case EventType.DragPerform: OnMouseDrag(e); break;
            }
        }

        private void OnMouseMove(Event e) {
            if (!e.alt && e.button == 0) {
                toolkit.OnMouseMove(e);
                SceneView.RepaintAll();
            }
        }

        private void OnMouseDown(Event e) {
            if (!e.alt && e.button == 0) {
                toolkit.OnClic(e);
                SceneView.RepaintAll();
            }
        }

        private void OnMouseDrag(Event e) {
            if (!e.alt && e.button == 0) {
                toolkit.OnDrag(e);
                SceneView.RepaintAll();
            }
        }

        private void OnRepaint(Event e) {
            // draw hairdressing
            switch (toolkit.tool) {
                case HairTool.ChangeRoots:
                    // paint roots
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
                    DrawAllRoots();
                    // paint transparent guides
                    Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                    foreach (var guide in dressing.guides) {
                        DrawGuide(guide);
                    }
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                    break;
                case HairTool.AddGuide:
                case HairTool.Comb:
                case HairTool.Length:
                case HairTool.Select:
                    // paint guides
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
                    foreach (var guide in dressing.guides) {
                        Color c;
                        if (guide == toolkit.hovered) {
                            c = Color.magenta;
                        } else if (!toolkit.selection.Contains(guide)) {
                            c = Color.grey;
                        } else {
                            c = guide.mixedLock ? Color.cyan : Color.blue;
                        }
                        //Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, 0.4f);
                        Handles.color = c;
                        DrawGuide(guide);
                    }
                    // paint transparent roots
                    DrawAllRoots(0.3f);
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                    break;
            }

            // draw tool
            switch (toolkit.tool) {
                case HairTool.ChangeRoots:
                case HairTool.Comb:
                case HairTool.Length:
                    if (!e.alt) {
                        // drawing tool
                        Handles.BeginGUI();
                        Handles.color = new Color(0.2f, 0.2f, 1, 0.2f);
                        Handles.DrawSolidDisc(toolkit.toolPosition, Vector3.forward, toolkit.toolRadius);
                        Handles.color = new Color(1f, 0.2f, 0.2f, 0.3f);
                        Handles.DrawSolidDisc(toolkit.toolPosition, Vector3.forward, toolkit.toolRadius * 0.95f * toolkit.toolStrength);
                        Handles.EndGUI();
                    }
                    break;
                case HairTool.AddGuide:
                case HairTool.Select:
                    break;
            }
        }

        public void DrawGuide(Guide guide) {
            //var points = MathUtility.GetSmoothPath(guide.segments.Select(seg => locToW(seg.localPosition)).ToList(), 5);
            Handles.DrawPolyLine(guide.segments.Select(seg => locToW(seg.localPosition)).ToArray());
        }

        public override void OnInspectorGUI() {
            DrawCommonProperties();

            // hair / root tool bar
            var newTab = GUILayout.Toolbar(tab,
                new GUIContent[2]{
                new GUIContent(hairIcon, "Style the hair by combing the guides."),
                new GUIContent(rootIcon, "Draw the scalp area onto the scalp collider."),
                }, GUILayout.Height(40));
            if (newTab != tab) {
                tab = newTab;
                ChangeTool(tab == 0 ? HairTool.Comb : HairTool.ChangeRoots);
            }

            // tab page
            tabPageStyle = new GUIStyle(GUI.skin.box);
            tabPageStyle.padding = new RectOffset(10, 10, 10, 10);
            GUILayout.BeginVertical(tabPageStyle);
            if (tab == 0) DrawCombTab();
            else DrawScalpTab();
            GUILayout.EndVertical();
        }

        private void DrawCommonProperties() {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hairDensity"));

            // clumping
            clumpingExpanded = EditorGUILayout.Foldout(clumpingExpanded, new GUIContent("Clumping", "This effect pull the hair in the direction of its closest guide to add volume and avoid parallelism."), true);
            if (clumpingExpanded) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minClumping"), new GUIContent("Min"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxClumping"), new GUIContent("Max"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("clumpingAlongStrand"), new GUIContent("Curve"));
                EditorGUI.indentLevel--;
            }

            // waviness
            wavinessExpanded = EditorGUILayout.Foldout(wavinessExpanded, new GUIContent("Waviness", "A random displacement of the strand segments to simulate waviness"), true);
            if (wavinessExpanded) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("waviness"), new GUIContent("Amplitude"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wavinessAlongStrand"), new GUIContent("Amplitude curve"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wavinessFrequency"), new GUIContent("Frequency"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wavinessFrequencyAlongStrand"), new GUIContent("Frequency curve"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSegmentCount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("randomSeed"));
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawScalpTab() {
            EditorGUILayout.HelpBox(
                "Draw the scalp area onto the scalp collider.\n" +
                "Clic to place roots. Hold shift to remove roots.\n" +
                "Hold control and move the mouse to control tool's size and strength.",
                MessageType.None);

            rootZoneIndex = GUILayout.SelectionGrid(rootZoneIndex,
                new GUIContent[6]{
                new GUIContent("A", "Place roots for zone A."),
                new GUIContent("AB", "Place roots for both zones A and B."),
                new GUIContent("B", "Place roots for zone B."),
                new GUIContent("BC", "Place roots for both zones B and C."),
                new GUIContent("C", "Place roots for zone C."),
                new GUIContent("CA", "Place roots for both zones C and A."),
                }, 6);

            switch (rootZoneIndex) {
                case 0: toolkit.rootZone = Root.A; break;
                case 1: toolkit.rootZone = Root.AB; break;
                case 2: toolkit.rootZone = Root.B; break;
                case 3: toolkit.rootZone = Root.BC; break;
                case 4: toolkit.rootZone = Root.C; break;
                case 5: toolkit.rootZone = Root.CA; break;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("scalpCollider"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rootRadius"));
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Remove all roots")) {
                if (EditorUtility.DisplayDialog("Remove roots", "Removing all roots can't be undone.", "Remove all roots", "Cancel")) {
                    dressing.roots.RemoveAll();
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.LabelField("Current root count : " + dressing.roots.Get().Count);
        }

        private void DrawCombTab() {
            EditorGUILayout.HelpBox(
                "Style the hair by combing the guides.\n" +
                "Hold control and move the mouse to control tool's size and strength.",
                MessageType.None);

            // tool selector
            var newToolIndex = GUILayout.SelectionGrid(toolIndex,
                new GUIContent[4]{
                new GUIContent(combIcon, "Comb guides to shape the hair style."),
                new GUIContent(scissorIcon, "Trim and grow (hold shift) the guides."),
                new GUIContent(selectIcon, "Select or unselect guides."),
                new GUIContent(addIcon, "Add guides.")
                }, 4, GUILayout.Height(40));
            if (newToolIndex != toolIndex) {
                toolIndex = newToolIndex;
                switch (toolIndex) {
                    case 0: ChangeTool(HairTool.Comb); break;
                    case 1: ChangeTool(HairTool.Length); break;
                    case 2: ChangeTool(HairTool.Select); break;
                    case 3: ChangeTool(HairTool.AddGuide); break;
                }
            }

            // selection buttons
            if (toolkit.tool == HairTool.Select) {
                Rect r = EditorGUILayout.BeginHorizontal();
                // select all
                // enabled if not all guide are already selected
                GUI.enabled = dressing.guides.Except(toolkit.selection).Any();
                if (GUILayout.Button(new GUIContent(selectAllIcon, "Select all guides."),
                    GUILayout.Width(40),
                    GUILayout.Height(40))) {
                    toolkit.selection.Clear();
                    toolkit.selection.AddRange(dressing.guides);
                    SceneView.RepaintAll();
                }
                // select none
                // enable if something is selected
                GUI.enabled = toolkit.selection.Any();
                if (GUILayout.Button(new GUIContent(selectNoneIcon, "Unselect all guides."),
                    GUILayout.Width(40),
                    GUILayout.Height(40))) {
                    toolkit.selection.Clear();
                    SceneView.RepaintAll();
                }
                // delete selected
                if (GUILayout.Button(new GUIContent(deleteIcon, "Delete selected guides."),
                    GUILayout.Width(40),
                    GUILayout.Height(40))) {
                    dressing.guides = dressing.guides.Except(toolkit.selection).ToList();
                    toolkit.selection.Clear();
                    SceneView.RepaintAll();
                }
                // toggle interpolation
                var areAllSingleLocks = toolkit.selection.All(g => !g.mixedLock);
                var content = areAllSingleLocks ?
                    new GUIContent(interpolationOnIcon, "Turn interpolation on for selected guides, allowing for smooth strands between guides. Interpolating between very differents guides will lead to mad strands or instable simulation.") :
                    new GUIContent(interpolationOffIcon, "Turn interpolation off for selected guides, so each guide will result in a single lock of hair.");
                if (GUILayout.Button(content,
                    GUILayout.Width(40),
                    GUILayout.Height(40))) {
                    foreach (var guide in toolkit.selection) {
                        guide.mixedLock = areAllSingleLocks;
                    }
                    SceneView.RepaintAll();
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }

            // colliders
            EditorGUILayout.PropertyField(serializedObject.FindProperty("colliders"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("scalpSpacing"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("scalpSpacingAtTip"));

            // guide generation
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            guideGeneratorFoldout = EditorGUILayout.Foldout(guideGeneratorFoldout, "Guide generator", true);
            if (guideGeneratorFoldout) {
                guideCount = EditorGUILayout.IntField(
                    new GUIContent("Number of guides", "The number of guides to generate. Set 0 here to use density instead."),
                    guideCount);

                guideDensity = EditorGUILayout.Slider(
                    new GUIContent("Density of guides", "The density of the guide, relative to the root count. Only used if the number of guides is set to 0."),
                    guideDensity, 0, 1);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("segmentCountPerGuide"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("segmentLength"));
                if (GUILayout.Button(new GUIContent("Generate guides", "Delete existing guides and generate new guides with the following parameters"))) {
                    if(EditorUtility.DisplayDialog("Generate guides", "Generating guides will delete existing guides and can't be undone.", "Generate new guides", "Cancel")) {
                        toolkit.GenerateGuides(guideCount, guideDensity);
                    }
                }
            }
            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }

        private void ChangeTool(HairTool tool) {
            toolkit.tool = tool;
            SceneView.RepaintAll();
        }

        private Vector3 locToW(Vector3 v) {
            return dressing.transform.TransformPoint(v);
        }

        private void DrawAllRoots(float alpha = 1) {
            Handles.color = Color.red.WithAlpha(alpha);
            DrawRoots(dressing.roots.Get(Root.A));
            Handles.color = Color.Lerp(Color.red, Color.green, 0.5f).WithAlpha(alpha);
            DrawRoots(dressing.roots.Get(Root.AB));

            Handles.color = Color.green.WithAlpha(alpha);
            DrawRoots(dressing.roots.Get(Root.B));
            Handles.color = Color.Lerp(Color.green, Color.blue, 0.5f).WithAlpha(alpha);
            DrawRoots(dressing.roots.Get(Root.BC));

            Handles.color = Color.blue.WithAlpha(alpha);
            DrawRoots(dressing.roots.Get(Root.C));
            Handles.color = Color.Lerp(Color.blue, Color.red, 0.5f).WithAlpha(alpha);
            DrawRoots(dressing.roots.Get(Root.CA));
        }

        private void DrawRoots(IEnumerable<Root> roots) {
            var points = new List<Vector3>();
            foreach (var root in roots) {
                var wRoot = locToW(root.LocalPos);
                points.Add(wRoot);
                points.Add(wRoot + (wRoot - dressing.gameObject.transform.position).normalized * dressing.rootRadius);
            }
            Handles.DrawLines(points.ToArray());
        }
    }
}
