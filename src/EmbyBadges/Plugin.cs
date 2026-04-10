using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace EmbyBadges;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
{
    public static Plugin? Instance { get; private set; }

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    // Ne jamais changer ce GUID après le premier déploiement
    public override Guid Id => new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    public override string Name => "Emby Badges";

    public override string Description => "Affiche des badges de résolution et de langue sur les vignettes des médias.";

    public Stream GetThumbImage()
    {
        var type = GetType();
        return type.Assembly.GetManifestResourceStream($"{type.Namespace}.thumb.jpg")!;
    }

    public ImageFormat ThumbImageFormat => ImageFormat.Jpg;

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name                 = "EmbyBadgesConfig",
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html",
                EnableInMainMenu     = true,
                DisplayName          = "Emby Badges",
                MenuSection          = "server",
                MenuIcon             = "photo_filter"
            },
            new PluginPageInfo
            {
                Name                 = "EmbyBadgesConfigScript",
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configScript.js"
            }
        };
    }
}
