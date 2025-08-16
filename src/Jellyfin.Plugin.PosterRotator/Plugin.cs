using System;
using System.Collections.Generic;
using System.Globalization;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.PosterRotator;

public class Plugin : BasePlugin<Configuration>, IHasWebPages
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public override string Name => "Poster Rotator"; // keep this human-readable
    public override Guid Id => Guid.Parse("7f6eea8b-0e9c-4cbd-9d2a-31f9a37ce2b7");

    public IEnumerable<PluginPageInfo> GetPages()
    {
        var resource = string.Format(
            CultureInfo.InvariantCulture,
            "{0}.Configuration.config.html",
            GetType().Namespace);

        return new[]
        {
            // Template route: #/configurationpage?name=Poster%20Rotator
            new PluginPageInfo { Name = Name, EmbeddedResourcePath = resource },

            // Alias to support old cached links: #/configurationpage?name=posterrotator
            new PluginPageInfo { Name = "posterrotator", EmbeddedResourcePath = resource }
        };
    }
}
