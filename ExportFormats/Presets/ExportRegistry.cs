using System.Collections.Generic;
using UnityEngine;

namespace GameExport
{
    [CreateAssetMenu(fileName = "ExportRegistry", menuName = "Game Export/Export Registry", order = 0)]
    [Icon("Assets/B. GRAPHICS/ExportFormats/Presets/Sic_ExportBundle.png")]
    
    public class ExportRegistry : ScriptableObject
    {
        [Header("Global Output")]
        [Tooltip("Absolute or project-relative root folder. Example: Assets/Exports or C:/Builds/MyGame/Exports")]
        public string rootFolderPath;

        [Tooltip("Optional: Game/Product title (not used in filename pattern now, but kept for future).")]
        public string productTitle = "MyGame";

        [Header("Presets")]
        public List<ExportPreset> presets = new();

        public IEnumerable<ExportPreset> ValidPresets()
        {
            foreach (var p in presets)
                if (p != null) yield return p;
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(rootFolderPath))
                Debug.LogWarning("[ExportRegistry] Root folder path is empty.");
        }
    }
}