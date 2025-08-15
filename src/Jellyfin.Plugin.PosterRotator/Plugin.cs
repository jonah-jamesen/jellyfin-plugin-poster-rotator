using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Configuration;
using System;

namespace Jellyfin.Plugin.PosterRotator
{
    public class Plugin : BasePlugin<Configuration>
    {
        public static Plugin Instance { get; private set; }  // 👈

        public override string Name => "Poster Rotator";
        public override Guid Id => Guid.Parse("7f6eea8b-0e9c-4cbd-9d2a-31f9a37ce2b7");

        public Plugin(IApplicationPaths paths, IXmlSerializer serializer) : base(paths, serializer)
        {
            Instance = this; // 👈
        }
    }
}
