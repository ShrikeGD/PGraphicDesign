using System;
using System.Collections.Generic;
using UnityEditor.Presets;
using UnityEngine;

namespace GameExport
{
    [CreateAssetMenu(fileName = "ExportPreset", menuName = "Game Export/Export Preset", order = 1)]
    [Icon("Assets/B. GRAPHICS/ExportFormats/Presets/Sic_ExportPreset.png")]
    public class ExportPreset : ScriptableObject
    {
        [Header("Preset Identity")]
        public PresetType presetType = PresetType.General;
        [Tooltip("Optional override to use in UI instead of the asset name.")]
        public string presetNameOverride;

        [Header("Entries (sizes)")]
        public List<ExportEntry> entries = new();

        public string GetDisplayName() =>
            string.IsNullOrEmpty(presetNameOverride) ? name : presetNameOverride;

        private void OnValidate()
        {
            if (entries == null) return;
            for (int i = 0; i < entries.Count; i++)
                entries[i].Validate($"{GetDisplayName()} [{i}]");
        }
    }

    [Serializable]
    public struct ExportEntry
    {
        [Tooltip("Human-friendly tag (e.g., MainCapsule, LibraryHero, Story, OG_Link).")]
        public string entryName;
        
        [Tooltip("Any production notes for this entry.")]
        [TextArea(1, 4)] public string note;

        [Tooltip("Pixel resolution (Vector2). Use whole numbers.")]
        public Vector2 resolution;

        [Tooltip("Target file/container format.")]
        public FileFormat fileFormat;
        
        public ExportTag entryTagMask;


        [Tooltip("Duration in seconds (GIF/MP4). Ignored for static images.")]
        public float duration;

        

        public int WidthInt => Mathf.RoundToInt(resolution.x);
        public int HeightInt => Mathf.RoundToInt(resolution.y);

        public void Validate(string contextLabel)
        {
            if (resolution.x <= 0 || resolution.y <= 0)
                Debug.LogError($"[ExportPreset] {contextLabel}: Resolution must be > 0.");

            // Encourage integer pixels
            if (!Mathf.Approximately(resolution.x, Mathf.Round(resolution.x)) ||
                !Mathf.Approximately(resolution.y, Mathf.Round(resolution.y)))
            {
                Debug.LogWarning($"[ExportPreset] {contextLabel}: Resolution should be whole-number pixels (currently {resolution}).");
            }

            if ((fileFormat == FileFormat.GIF || fileFormat == FileFormat.MP4) && duration <= 0f)
            {
                Debug.LogWarning($"[ExportPreset] {contextLabel}: Non-positive duration for animated/video format.");
            }
        }
    }
}
