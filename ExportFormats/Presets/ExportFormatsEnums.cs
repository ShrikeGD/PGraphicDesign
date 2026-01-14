using System;
using UnityEngine;

namespace GameExport
{
  
    /// <summary>Where this preset is meant to publish (for organization & filenames).</summary>
    public enum PresetType
    {
        General,
        SteamPage,
        SteamBlog,
        SteamLibrary,
        Xbox,
        PlayStation,
        NintendoSwitch,
        Social,
        Email,
        YouTube,
        Custom,
        Achievement
    }

    /// <summary>Export file/container type for each entry.</summary>
    public enum FileFormat
    {
        JPG,
        PNG, //with alpha, default
        PNG24, //playstation wants without alpha ..
        ICO,
        GIF,
        MP4
    }
}