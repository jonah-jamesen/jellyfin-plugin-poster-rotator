using MediaBrowser.Model.Plugins;
using System.Collections.Generic;

namespace Jellyfin.Plugin.PosterRotator;

public class Configuration : BasePluginConfiguration
{
    public List<string> Libraries { get; set; } = new();
    public int  PoolSize { get; set; } = 5;
    public bool SequentialRotation { get; set; } = true;
    public bool SaveNextToMedia { get; set; } = true;
    public bool LockImagesAfterFill { get; set; } = false;
    public bool DryRun { get; set; } = false;
    public List<string> ExtraPosterPatterns { get; set; } = new();
    public int MinHoursBetweenSwitches { get; set; } = 23;
}
