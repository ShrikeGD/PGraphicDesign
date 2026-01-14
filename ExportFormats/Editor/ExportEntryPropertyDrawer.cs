#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameExport
{
    [CustomPropertyDrawer(typeof(ExportEntry))]
    public class ExportEntryDrawer : PropertyDrawer
    {
        const float PAD = 2f;

        // Extra breathing room between list elements
        const float VSPACE_TOP = 4f;
        const float VSPACE_BOTTOM = 6f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;

            // Layout:
            // 1) EntryName + TagMask + FileFormat
            // 2) Note (compact text area)
            // 3) Resolution X/Y + DontOverwrite (+ Duration if needed)
            float h = 0f;

            h += line + PAD;                // row 1
            h += (line * 1.6f) + PAD;       // note
            h += line + PAD;                // row 3

            // Add top/bottom spacing between list elements
            h += VSPACE_TOP + VSPACE_BOTTOM;

            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Apply vertical padding
            position.y += VSPACE_TOP;
            position.height -= (VSPACE_TOP + VSPACE_BOTTOM);

            var entryName      = property.FindPropertyRelative("entryName");
            var note           = property.FindPropertyRelative("note");
            var resolution     = property.FindPropertyRelative("resolution");
            var fileFormat     = property.FindPropertyRelative("fileFormat");
            var entryTagMask   = property.FindPropertyRelative("entryTagMask");
            var duration       = property.FindPropertyRelative("duration");
            var dontOverwrite  = property.FindPropertyRelative("dontOverwrite");

            float line = EditorGUIUtility.singleLineHeight;
            var r = position;
            r.height = line;

            // ---- Row 1: EntryName | TagMask | FileFormat (colored) ----
            float wName = r.width * 0.56f;
            float wTag  = r.width * 0.26f;
            float wFmt  = r.width - wName - wTag - 8f;

            var nameRect = new Rect(r.x, r.y, wName, line);
            var tagRect  = new Rect(nameRect.xMax + 4f, r.y, wTag, line);
            var fmtRect  = new Rect(tagRect.xMax + 4f, r.y, wFmt, line);

            EditorGUI.PropertyField(nameRect, entryName, GUIContent.none);
            EditorGUI.PropertyField(tagRect, entryTagMask, GUIContent.none);

            // Color the format field like ExportWindowUI
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = GetFormatColor(fileFormat);
            EditorGUI.PropertyField(fmtRect, fileFormat, GUIContent.none);
            GUI.backgroundColor = prevBg;

            // ---- Row 2: Note (text area) ----
            r.y += line + PAD;
            r.height = line * 1.6f;
            note.stringValue = EditorGUI.TextArea(r, note.stringValue);

            // ---- Row 3: Resolution X/Y | DontOverwrite (+ red IGNORE) | Duration (GIF/MP4 only) ----
            r.y += r.height + PAD;
            r.height = line;

            float wRes = r.width * 0.46f;

            // dontOverwrite region
            float wTog = r.width * 0.20f;

            // duration region (or empty if hidden)
            float wDur = r.width - wRes - wTog - 8f;

            var resRect = new Rect(r.x, r.y, wRes, line);
            var togRect = new Rect(resRect.xMax + 4f, r.y, wTog, line);
            var durRect = new Rect(togRect.xMax + 4f, r.y, wDur, line);

            DrawResolutionXY(resRect, resolution);

            // Tooltip over the bool toggle (uses your [Tooltip] text)
            var ignoreContent = new GUIContent("", dontOverwrite.tooltip);
            EditorGUI.PropertyField(togRect, dontOverwrite, ignoreContent);

            // Red IGNORE text when enabled (right behind it)
            if (dontOverwrite.boolValue)
            {
                // draw just to the right of the toggle rect
                var ignoreRect = new Rect(togRect.xMax + 4f, r.y, 70f, line);
                var prev = GUI.color;
                GUI.color = Color.white;
                GUI.Label(ignoreRect, "Custom", EditorStyles.boldLabel);
                GUI.color = prev;

                // shift duration right a bit to avoid overlap if space is tight
                durRect.x += 50f;
                durRect.width -= 50f;
            }

            // Only show duration for GIF/MP4
            if (IsGifOrMp4(fileFormat))
            {
                EditorGUI.PropertyField(durRect, duration, GUIContent.none);
            }

            EditorGUI.EndProperty();
        }

        private static void DrawResolutionXY(Rect rect, SerializedProperty resolutionProp)
        {
            var xProp = resolutionProp.FindPropertyRelative("x");
            var yProp = resolutionProp.FindPropertyRelative("y");

            float line = EditorGUIUtility.singleLineHeight;

            float wLabel = 14f;
            float wField = (rect.width - (wLabel * 2f) - 6f) / 2f;

            var xLabel = new Rect(rect.x, rect.y, wLabel, line);
            var xField = new Rect(xLabel.xMax, rect.y, wField, line);

            var yLabel = new Rect(xField.xMax + 6f, rect.y, wLabel, line);
            var yField = new Rect(yLabel.xMax, rect.y, wField, line);

            EditorGUI.LabelField(xLabel, "X");
            xProp.floatValue = Mathf.Max(0, EditorGUI.IntField(xField, Mathf.RoundToInt(xProp.floatValue)));

            EditorGUI.LabelField(yLabel, "Y");
            yProp.floatValue = Mathf.Max(0, EditorGUI.IntField(yField, Mathf.RoundToInt(yProp.floatValue)));
        }

        private static bool IsGifOrMp4(SerializedProperty fileFormatProp)
        {
            string enumName = fileFormatProp.enumDisplayNames[fileFormatProp.enumValueIndex];
            return enumName == "GIF" || enumName == "MP4";
        }

        // Match ExportWindowUI.GetFormatColor
        private static Color GetFormatColor(SerializedProperty fileFormatProp)
        {
            string enumName = fileFormatProp.enumDisplayNames[fileFormatProp.enumValueIndex];

            switch (enumName)
            {
                case "JPG": return new Color(0.156f, 0.727f, 0.869f);
                case "PNG": return new Color(0.314f, 0.537f, 1.0f);
                case "ICO": return new Color(0.965f, 0.367f, 0.110f);
                case "GIF": return new Color(0.460f, 0.867f, 0.375f);
                case "MP4": return new Color(0.927f, 0.980f, 0.865f);
                default:    return new Color(0.85f, 0.85f, 0.85f);
            }
        }
    }
}
#endif
