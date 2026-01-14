// ===============================
// =====   FILE: ExportPaths.cs   =====
// ===============================
// Assets/Editor/GameExport/ExportPaths.cs
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameExport
{
    /// <summary>Path building, naming, and utility helpers.</summary>
    public static class ExportPaths
    {
        public static string ResolveRootFolder(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                string fallback = Path.Combine(Application.dataPath, "../Exports");
                Directory.CreateDirectory(fallback);
                return Path.GetFullPath(fallback);
            }
            string candidate = input.Replace("\\", "/");
            if (!Path.IsPathRooted(candidate))
            {
                string abs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", candidate));
                return abs;
            }
            return Path.GetFullPath(candidate);
        }

        public static string GetPresetFolder(string root, PresetType type)
        {
            string dir = Path.Combine(root, type.ToString());
            EnsureDir(dir);
            return dir;
        }

        public static string BuildFileName(PresetType presetType, ExportEntry e, string currentExportName)
        {
            string entryStr = Sanitize(e.entryName);
            string resStr = $"{Mathf.RoundToInt(e.resolution.x)}x{Mathf.RoundToInt(e.resolution.y)}";
            string ext = GetExt(e.fileFormat);

            if (string.IsNullOrWhiteSpace(currentExportName))
                return $"{entryStr}_{resStr}.{ext}";

            string suffix = Sanitize(currentExportName);
            return $"{entryStr}_{resStr}_{suffix}.{ext}";
        }


        public static void OpenFolder(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath) || !Directory.Exists(absolutePath))
            {
                Debug.LogWarning($"[Export] Folder does not exist: {absolutePath}");
                return;
            }
            EditorUtility.RevealInFinder(absolutePath);
        }

        // --- internals ---
        public static void EnsureDir(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        public static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "Unnamed";
            foreach (char c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s.Trim();
        }

        public static string GetExt(FileFormat f)
        {
            return f switch
            {
                FileFormat.JPG => "jpg",
                FileFormat.PNG => "png",
                FileFormat.PNG24 => "png", // Add this
                FileFormat.ICO => "ico",
                FileFormat.GIF => "gif",
                FileFormat.MP4 => "mp4",
                _ => "dat"
            };
        }
    }
}
#endif
