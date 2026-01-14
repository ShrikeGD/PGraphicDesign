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
                padding = new RectOffset(8, 8, 6, 6) // a touch tighter to reduce “mystery gaps”
            };

            Bold = new GUIStyle(EditorStyles.boldLabel);
        }

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
                        // Optional Play Mode convenience capture (kept)
                        if (Application.isPlaying)
                        {
                            if (GUILayout.Button("Export (Play Mode Screenshot)", GUILayout.Height(22)))
                                ExportService.ExportPreset_PlayModeScreenCapture(registry, preset, currentExportName, windowTagMask);
                        }

                        // =========================
                        // New layout:
                        // 2300 x 2000 PNG | Name | Tags                              [aspect] [rect] [tiny export-one]
                        // Description
                        // =========================

                        bool isCustom = e.dontOverwrite;

                        Color fmtColor = GetFormatColor(e.fileFormat);
                        Color disabledGray = new Color(0.55f, 0.55f, 0.55f, 1f);

                        // Styles
                        var nameStyle = new GUIStyle(EditorStyles.boldLabel);
                        var leftStyle = new GUIStyle(EditorStyles.label);
                        var tagStyle = SmallTagStyle();

                        // Tint colors (custom => all medium gray)
                        if (isCustom)
                        {
                            nameStyle.normal.textColor = disabledGray;
                            leftStyle.normal.textColor = disabledGray;
                            tagStyle.normal.textColor = disabledGray;
                        }
                        else
                        {
                            // Name keeps default (theme), res/ext uses fmt color
                            leftStyle.normal.textColor = fmtColor;
                        }

                        var faintStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            alignment = TextAnchor.MiddleRight
                        };
                        faintStyle.normal.textColor = new Color(0.35f, 0.35f, 0.35f, 1f);

                        // Tiny button style (compact)
                        var tinyBtnStyle = new GUIStyle(EditorStyles.miniButton)
                        {
                            padding = new RectOffset(2, 2, 1, 1),
                            margin = new RectOffset(2, 0, 0, 0)
                        };

                        // ----- LINE 1 -----
                        float line = EditorGUIUtility.singleLineHeight;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            // Left chunk: "W x H EXT | Name | Tags"
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                int w = Mathf.RoundToInt(e.resolution.x);
                                int h = Mathf.RoundToInt(e.resolution.y);

                                // "2300 x 2000 PNG"
                                string ext = ExportPaths.GetExt(e.fileFormat).ToUpperInvariant();
                                GUILayout.Label($"{w} x {h} {ext}", isCustom ? EditorStyles.label : leftStyle);

                                GUILayout.Space(2);
                                GUILayout.Label("⭢", EditorStyles.miniLabel);
                                GUILayout.Space(2);

                                // Name (bold)
                                GUILayout.Label(e.entryName, nameStyle);

                                // Tags
                                if (e.entryTagMask != ExportTag.None)
                                {
                                    GUILayout.Label("", EditorStyles.miniLabel);
                                    GUILayout.Label(e.entryTagMask.ToString(), tagStyle);
                                }
                            }

                            GUILayout.FlexibleSpace();

                            // Right chunk: aspect ratio + preview rect (fixed width) + tiny export-one button
                            Rect right = GUILayoutUtility.GetRect(120f, line, GUILayout.ExpandWidth(false));
                            DrawAspectAndPreviewRect(
                                right,
                                Mathf.RoundToInt(e.resolution.x),
                                Mathf.RoundToInt(e.resolution.y),
                                faintStyle,
                                isCustom);

                            // Tiny per-entry export button (Edit Mode export of just this entry)
                            // Uses a small icon-like label to keep it unobtrusive.
                            var exportOneContent = new GUIContent("⤓", "Export only this entry");
                            if (GUILayout.Button(exportOneContent, tinyBtnStyle, GUILayout.Width(22f), GUILayout.Height(line)))
                            {
                                // Export only this one entry (same semantics as preset export)
                                ExportService.ExportSingleEntryAndGetFolder(
                                    registry,
                                    preset,
                                    e,
                                    currentExportName,
                                    windowTagMask,
                                    openFolderAfter: false
                                );
                            }
                        }

                        // ----- LINE 2 (Description) -----
                        if (!string.IsNullOrEmpty(e.note))
                        {
                            GUILayout.Label(e.note, EditorStyles.wordWrappedLabel);
                        }
                    }
                }
            }
        }

        private static void DrawAspectAndPreviewRect(
            Rect rightRect,
            int w,
            int h,
            GUIStyle aspectStyle,
            bool isCustom)
        {
            if (w <= 0 || h <= 0) return;

            // aspect ratio string (simplified)
            string aspect = AspectString(w, h);

            // Determine scale indicator for small sizes
            float scale = 1f;
            if (w < 500 && h < 500) scale = 0.25f;
            else if (w < 1000 && h < 1000) scale = 0.5f;

            const float gap = 6f;
            float boxMax = 32f * scale;

            boxMax = Mathf.Min(boxMax, rightRect.height);
            boxMax = Mathf.Min(boxMax, rightRect.width);

            float aspectW = Mathf.Clamp(rightRect.width - boxMax - gap, 24f, 64f);

            Rect aspectRect = new Rect(
                rightRect.x,
                rightRect.y,
                aspectW,
                rightRect.height);

            Rect boxRect = new Rect(
                aspectRect.xMax + gap,
                rightRect.y + (rightRect.height - boxMax) * 0.5f,
                boxMax,
                boxMax);

            GUI.Label(aspectRect, aspect, aspectStyle);

            float ar = (h == 0) ? 1f : (float)w / h;

            float innerW, innerH;
            if (ar >= 1f)
            {
                innerW = boxRect.width;
                innerH = boxRect.width / ar;
            }
            else
            {
                innerH = boxRect.height;
                innerW = boxRect.height * ar;
            }

            Rect inner = new Rect(
                boxRect.x + (boxRect.width - innerW) * 0.5f,
                boxRect.y + (boxRect.height - innerH) * 0.5f,
                innerW,
                innerH);

            Color fill = isCustom
                ? new Color(0.60f, 0.60f, 0.60f, 0.08f)
                : new Color(0.60f, 0.60f, 0.60f, 0.12f);

            Color border = isCustom
                ? new Color(0.60f, 0.60f, 0.60f, 0.20f)
                : new Color(0.60f, 0.60f, 0.60f, 0.28f);

            EditorGUI.DrawRect(inner, fill);
            DrawRectOutline(inner, border, 1f);
        }

        private static void DrawRectOutline(Rect r, Color c, float thickness)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, thickness), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - thickness, r.width, thickness), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, thickness, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - thickness, r.y, thickness, r.height), c);
        }

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
                case FileFormat.JPG: return new Color(0.156f, 0.727f, 0.869f);
                case FileFormat.PNG: return new Color(0.314f, 0.537f, 1.0f);
                case FileFormat.PNG24: return new Color(0.314f, 0.537f, 1.0f);
                case FileFormat.ICO: return new Color(0.965f, 0.367f, 0.110f);
                case FileFormat.GIF: return new Color(0.460f, 0.867f, 0.375f);
                case FileFormat.MP4: return new Color(0.927f, 0.980f, 0.865f);
                default:             return new Color(0.85f, 0.85f, 0.85f);
            }
        }

        private static string AspectString(int w, int h)
        {
            if (w <= 0 || h <= 0) return "";
            int g = GCD(w, h);
            return $"{w / g}:{h / g}";
        }

        private static int GCD(int a, int b)
        {
            a = Mathf.Abs(a);
            b = Mathf.Abs(b);
            while (b != 0)
            {
                int t = a % b;
                a = b;
                b = t;
            }
            return Mathf.Max(1, a);
        }
    }
}
#endif
// --- END FILE: ExportWindowUI.cs ---
