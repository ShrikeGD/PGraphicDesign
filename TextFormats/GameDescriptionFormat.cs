using UnityEngine;

public class ExportFormat : ScriptableObject
{
    public string gameName;
    [Tooltip("A short description of the game, like Tactical Rogue-lite shooter. Mandatory on Playstation network at the least (80 Chars max)")]
    public string subHeader = "A interesting game concept shooter";
    
    [Tooltip("The short description. Max ")]
    public string descriptionShort = "SYNTHETIK 2 is a unrelenting tactical-shooter rogue-lite. Play alone or with friends. Loot powerful tech. " +
                                     "Experience the next level in gun-play. Upgrade your androids to rival the machine gods!";
    
    [Tooltip("The tags for the game. Mandatory on steam and playstation. On Steam you need to look up existing tags")]
    public string tags = "Shooter, Rogue-Lite, PVE, Online Co-Op";
    
    
    [Tooltip("The tags for the game. Mandatory on steam and playstation. On Steam you need to look up existing tags")]
    public string legal = "Legal information";

    
    
    
    [Space] 
    [Header("URLs")]
    public string website = "https://www.synthetik2.com/";
    public string discord = "https://Discord.gg/synthetik";
    public string twitter = "https://x.com/SynthetikGame";
    public string youtube = "https://www.youtube.com/@flowfiregames1338";
    public string reddit = "https://www.reddit.com/r/synthetik/";
}
