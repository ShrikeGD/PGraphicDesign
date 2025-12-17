#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace GameExport
{
    /// <summary>Enables/disables objects marked with ExportTagMarker during export, with restore.</summary>
    public static class ExportTagMasking
    {
        // was: private struct TagState
        public struct TagState
        {
            public GameObject go;
            public bool wasActive;
        }

        public static List<TagState> SnapshotAllTags()
        {
            var list = new List<TagState>();
            var markers = Object.FindObjectsOfType<ExportTagMarker>(true);
            foreach (var m in markers)
                list.Add(new TagState { go = m.gameObject, wasActive = m.gameObject.activeSelf });
            return list;
        }

        /// <summary>Apply mask without snapshot. Use SnapshotAllTags() first and RestoreTagSnapshot() afterward.</summary>
        public static void SetTagMaskNoSnapshot(ExportTag enabledMask)
        {
            var markers = Object.FindObjectsOfType<ExportTagMarker>(true);
            foreach (var m in markers)
            {
                bool shouldShow = (m.tags & enabledMask) != 0;
                if (m.gameObject.activeSelf != shouldShow)
                    m.gameObject.SetActive(shouldShow);
            }
        }

        public static void RestoreTagSnapshot(List<TagState> snapshot)
        {
            if (snapshot == null) return;
            foreach (var s in snapshot)
            {
                if (s.go != null && s.go.activeSelf != s.wasActive)
                    s.go.SetActive(s.wasActive);
            }
        }
    }
}
#endif