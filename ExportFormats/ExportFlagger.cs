using System;
using UnityEngine;

namespace GameExport
{
    // Hard-typed, flaggable tags you can toggle during export.
    [Flags]
    public enum ExportTag
    {
        None = 0,
        Logo = 1 << 0,
        Text = 1 << 1,
        FX   = 1 << 2,
        // Add more if needed, e.g.,
        // Characters = 1 << 3,
        // UI = 1 << 4,
    }

    /// <summary>
    /// Drop this on any GameObject you want controlled by the export tag mask.
    /// If any bit in 'tags' matches the enabled mask, the object is shown; otherwise hidden during capture.
    /// </summary>
    public class ExportTagMarker : MonoBehaviour
    {
        public ExportTag tags = ExportTag.None;
    }
}