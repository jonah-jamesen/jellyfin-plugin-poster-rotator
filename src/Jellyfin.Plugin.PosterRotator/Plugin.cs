using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.PosterRotator
{
    public class Plugin : BasePlugin<Configuration>, IHasWebPages
    {
        // Satisfy nullable analyzer and allow static access in tasks
        public static Plugin Instance { get; private set; } = null!;

        public override string Name => "Poster Rotator";
        public override Guid Id => Guid.Parse("7f6eea8b-0e9c-4cbd-9d2a-31f9a37ce2b7");

        public Plugin(IApplicationPaths paths, IXmlSerializer serializer) : base(paths, serializer)
        {
            Instance = this;
        }

        // Tell Jellyfin where our settings page lives
        public IEnumerable<PluginPageInfo> GetPages()
        {
            // The resource path is "<Namespace>.Web.config.html"
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "posterrotator", // used in the settings URL
                    EmbeddedResourcePath = GetType().Namespace + ".Web.config.html"
                }
            };
        }
    }
}
