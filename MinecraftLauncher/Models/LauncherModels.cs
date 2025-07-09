using System;
using System.Collections.Generic;

namespace MinecraftLauncher.Models
{
    public class LauncherConfig
    {
        public string PlayerName { get; set; }
        public string GameVersion { get; set; }
        public int MaxMemory { get; set; }
        public int MinMemory { get; set; }
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }
        public string JvmArguments { get; set; }
        public string LauncherTitle { get; set; }
        public string LauncherSubtitle { get; set; }
        public string FooterText { get; set; }
        public string ServerUrl { get; set; }
        public string JavaPath { get; set; }
    }

    public class GameVersion
    {
        public string Version { get; set; }
        public string Description { get; set; }
        public DateTime ReleaseDate { get; set; }
        public List<GameFile> Files { get; set; }
        public string MinecraftVersion { get; set; }
        public string ForgeVersion { get; set; }
        public List<string> RequiredMods { get; set; }
    }

    public class GameFile
    {
        public string Path { get; set; }
        public string Url { get; set; }
        public string Hash { get; set; }
        public long Size { get; set; }
        public bool IsRequired { get; set; }
        public string Type { get; set; } // mod, config, resource, library
        public string Description { get; set; }
    }

    public class DownloadProgress
    {
        public int Percentage { get; set; }
        public string CurrentFile { get; set; }
        public string Message { get; set; }
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
    }
}
