#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameExport
{
    public class ExportWindow : EditorWindow
    {
        private ExportRegistry registry;
        private Vector2 presetsScroll;
        private string currentExportName = "";
        private ExportTag enabledTagMask = ExportTag.Logo | ExportTag.Text | ExportTag.FX;

        // Foldouts (keep names for searchability)
        private bool _advancedFoldout;

        // ----- Styles (keep variable names)
        GUIStyle _wrapper;
        GUIStyle _mainBox;
        GUIStyle _statsBox;

        [MenuItem("Export Graphics/Export Window")]
        public static void Open()
        {
            var win = GetWindow<ExportWindow>("Export Window");
            win.minSize = new Vector2(720, 520);
        }
        
        private void OnGUI()
        {
            // A wrapper style to add a margin to the left and right of the entire inspector.
            _wrapper = new GUIStyle()
            {
                padding = new RectOffset(0, 8, 0, 0)
            };

            // This is the main style used and wrapped around groups of objects
            _mainBox = new GUIStyle("helpBox")
            {
                padding = new RectOffset(12, 12, 12, 12),
                margin = new RectOffset(0, 0, 12, 12),
            };

            // This is for read only info or stats displays, mostly at the bottom.
            _statsBox = new GUIStyle("box")
            {
                padding = new RectOffset(12, 12, 12, 12),
                margin = new RectOffset(0, 0, 12, 12),
            };

            using (new GUILayout.VerticalScope(_wrapper))
            {
                // ===== Group: Registry & Quick Actions =====
                using (new GUILayout.VerticalScope(_mainBox))
                {
                    // Light green tint for the main group box (subtle)
                    Color tintColor = new Color(0.90f, 1.00f, 0.92f, 1.0f); // light green tint
                    Color originalColor = GUI.backgroundColor;
                    GUI.backgroundColor = tintColor;

                    registry = (ExportRegistry)EditorGUILayout.ObjectField(
                        new GUIContent("Export Registry", "Assign the Export Registry asset that holds presets and the root output folder."),
                        registry, typeof(ExportRegistry), false);

                    GUI.backgroundColor = originalColor;

                    if (registry == null)
                    {
                        EditorGUILayout.HelpBox("Assign an Export Registry to proceed.", MessageType.Info);
                        return;
                    }

                    // Root path preview (read-only)
                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(new GUIContent("Root Folder", "Resolved absolute export root folder."), GUILayout.Width(110));
                        EditorGUILayout.TextField(ExportService.ResolveRootFolder(registry.rootFolderPath));
                    }

                    // Export options (compact, no headers)
                    using (new GUILayout.HorizontalScope())
                    {
                        // Current export name (no header)
                        currentExportName = EditorGUILayout.TextField(
                            new GUIContent("Current Export Name", "Optional suffix appended to file names (e.g., U15, V3). Leave empty to overwrite previous exports."),
                            currentExportName);

                        GUILayout.Space(12);

                        // Export tags (no header) with gray tint 0.7
                        Color prevBg = GUI.backgroundColor;
                        GUI.backgroundColor = Color.gray * 0.7f;
                        enabledTagMask = (ExportTag)EditorGUILayout.EnumFlagsField(
                            new GUIContent("Enabled Tags", "Toggle which tagged objects are visible during capture. Entries can override this with a per-asset tag mask."),
                            enabledTagMask);
                        GUI.backgroundColor = prevBg;
                    }

                    // Action row
                    using (new GUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(new GUIContent("Export ALL Presets", "Export every preset in the registry."), GUILayout.Height(26)))
                        {
                            string root = ExportService.ExportAllAndGetFolder(registry, currentExportName, enabledTagMask);
                            ExportService.OpenFolder(root);
                        }

                        GUILayout.FlexibleSpace();
                    }
                }

                // ===== Group: Presets List =====
                using (new GUILayout.VerticalScope(_mainBox))
                {
                    // Slight tint to differentiate the list group
                    Color listTint = new Color(0.96f, 0.98f, 1.00f, 1.0f);
                    Color prev = GUI.backgroundColor;
                    GUI.backgroundColor = listTint;

                    // Scrollable presets area
                    float desiredHeight = Mathf.Clamp(position.height - 240f, 240f, 1200f);
                    presetsScroll = EditorGUILayout.BeginScrollView(presetsScroll, GUILayout.Height(desiredHeight));

                    foreach (var preset in registry.ValidPresets())
                    {
                        // UPDATED: Passed 'enabledTagMask' as the 4th argument
                        ExportGUI.DrawPresetCard(registry, preset, currentExportName, enabledTagMask, p =>
                        {
                            string folder = ExportService.ExportPresetAndGetFolder(registry, p, currentExportName, enabledTagMask);
                            ExportService.OpenFolder(folder);
                        });
                        GUILayout.Space(8);
                    }

                    EditorGUILayout.EndScrollView();
                    GUI.backgroundColor = prev;
                }

                // ===== Stats / Info =====
                using (new GUILayout.VerticalScope(_statsBox))
                {
                    int presetCount = 0, entryCount = 0;
                    foreach (var p in registry.ValidPresets())
                    {
                        presetCount++;
                        if (p.entries != null) entryCount += p.entries.Count;
                    }

                    EditorGUILayout.LabelField(
                        new GUIContent("Summary", "Current registry totals for a quick scan."),
                        EditorStyles.boldLabel);

                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(new GUIContent("Presets", "Number of export presets in the registry."), GUILayout.Width(80));
                        EditorGUILayout.LabelField(presetCount.ToString(), GUILayout.Width(40));
                        GUILayout.Space(12);
                        EditorGUILayout.LabelField(new GUIContent("Entries", "Total export entries across all presets."), GUILayout.Width(80));
                        EditorGUILayout.LabelField(entryCount.ToString(), GUILayout.Width(60));
                        GUILayout.FlexibleSpace();
                    }
                }

                // ===== Less needed info / settings (foldout, bottom, outside boxes) =====
                _advancedFoldout = EditorGUILayout.Foldout(_advancedFoldout, new GUIContent("Additional Options", "Less frequently used info and tips."), true);
                if (_advancedFoldout)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(12); // indentation so it aligns with main boxes
                        using (new GUILayout.VerticalScope())
                        {
                            EditorGUILayout.HelpBox(
                                "• Filenames: PresetType_EntryName_WxH_[Suffix].ext\n" +
                                "• Suffix is the 'Current Export Name'. Leave empty to overwrite.\n" +
                                "• Per-asset tag masks override the global tag mask.\n" +
                                "• Recording GIF/MP4 in Play Mode captures motion across frames.\n" +
                                "• In Edit Mode, use the stepper (if enabled) to simulate between frames.",
                                MessageType.None);
                        }
                    }
                }

                // footnote
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox("Preset Name Override only affects display; filenames use PresetType, EntryName, Resolution, and optional Current Export Name.", MessageType.None);
            }
        }
    }
}
#endif
