// --- START FILE: ExportWindowUI.cs ---
// --- Path: ExportWindowUI.cs ---
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameExport
{
    public static class ExportGUI
    {
        static readonly GUIStyle PaddedBox;
        static readonly GUIStyle Bold;
        
        static ExportGUI()
        {
            PaddedBox = new GUIStyle("helpbox")
            {
                padding = new RectOffset(8, 8, 8, 8)
            };
            Bold = new GUIStyle(EditorStyles.boldLabel);
        }

        // UPDATED: Added 'ExportTag windowTagMask' to parameters
        public static void DrawPresetCard(
            ExportRegistry registry,
            ExportPreset preset,
            string currentExportName,
            ExportTag windowTagMask, 
            System.Action<ExportPreset> onExport)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"{preset.GetDisplayName()} ({preset.presetType})", Bold);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Export This Preset", GUILayout.Height(22)))
                        onExport?.Invoke(preset);

                    if (GUILayout.Button("Ping Asset", GUILayout.Width(100)))
                        EditorGUIUtility.PingObject(preset);
                }

                EditorGUILayout.Space(4);

                if (preset.entries == null) return;

                for (int i = 0; i < preset.entries.Count; i++)
                {
                    var e = preset.entries[i];

                    using (new EditorGUILayout.VerticalScope(PaddedBox))
                    {
                        if (Application.isPlaying)
                        {
                            // UPDATED: Now uses the passed 'windowTagMask'
                            if (GUILayout.Button("Export (Play Mode Screenshot)", GUILayout.Height(22)))
                                ExportService.ExportPreset_PlayModeScreenCapture(registry, preset, currentExportName, windowTagMask);
                        } 
                        
                        // Title
                        EditorGUILayout.LabelField($"{i + 1}. {e.entryName}", Bold);

                        // Resolution and Format
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            string resText = $"{Mathf.RoundToInt(e.resolution.x)} × {Mathf.RoundToInt(e.resolution.y)}";
                            GUILayout.Label(resText, EditorStyles.label);
                            GUILayout.FlexibleSpace();

                            var fmtStyle = new GUIStyle(EditorStyles.label);
                            fmtStyle.normal.textColor = GetFormatColor(e.fileFormat);
                            GUILayout.Label(e.fileFormat.ToString(), fmtStyle);
                        }

                        // Tags
                        if (e.entryTagMask != ExportTag.None)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.Label(TagPillText(e.entryTagMask), SmallTagStyle());
                                GUILayout.FlexibleSpace();
                            }
                        }

                        // Note
                        if (!string.IsNullOrEmpty(e.note))
                            EditorGUILayout.LabelField(e.note, EditorStyles.wordWrappedLabel);

                        // Preview path
                        string preview = ExportService.PreviewPath(registry, preset, e, currentExportName);
                        EditorGUILayout.LabelField(preview, EditorStyles.miniLabel);
                    }
                }
            }
        }

        private static string TagPillText(ExportTag mask) => $"Tags: {mask}";

        private static GUIStyle SmallTagStyle()
        {
            var s = new GUIStyle(EditorStyles.miniLabel);
            s.normal.textColor = new Color(0.55f, 0.75f, 0.95f);
            return s;
        }

        private static Color GetFormatColor(FileFormat f)
        {
            switch (f)
            {
                case FileFormat.JPG: return new Color(0.956f, 0.727f, 0.369f);
                case FileFormat.PNG: return new Color(0.965f, 0.367f, 0.110f);
                case FileFormat.ICO: return new Color(0.314f, 0.537f, 1.0f);
                case FileFormat.GIF: return new Color(0.460f, 0.867f, 0.375f);
                case FileFormat.MP4: return new Color(0.927f, 0.980f, 0.865f);
                default:             return new Color(0.85f, 0.85f, 0.85f);
            }
        }
    }
}
#endif
// --- END FILE: ExportWindowUI.cs ---