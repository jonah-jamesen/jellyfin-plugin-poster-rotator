using MediaBrowser.Model.Plugins;
using System.Collections.Generic;

namespace Jellyfin.Plugin.PosterRotator
{
    public class Configuration : BasePluginConfiguration
    {
        public string ServerUrl { get; set; } = "http://10.0.0.112:8096";
        public string ApiKey { get; set; } = "2a45d59c5b6d40268d1b87e4092b2a2a";
        public List<string> Libraries { get; set; } = new();
        public int PoolSize { get; set; } = 5;
        public bool SequentialRotation { get; set; } = true;
        public bool SaveNextToMedia { get; set; } = true;
        public bool LockImagesAfterFill { get; set; } = false;
        public bool DryRun { get; set; } = false;
        public List<string> ExtraPosterPatterns { get; set; } = new();
    }
}
