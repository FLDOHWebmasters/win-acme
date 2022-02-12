using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    [Plugin("7752c88f-2bd3-4d89-ad06-3c5c71d49475")]
    public class RemoteIISHelperOptions : InstallationPluginOptions<RemoteIISHelper>
    {
        public string? Host { get; set; }
        public long? SiteId { get; set; }
        public string? NewBindingIp { get; set; }
        public int? NewBindingPort { get; set; }

        public override string Name => "IIS Helper";
        public override string Description => "Create or update https bindings in a remote IIS via helper app";
        public override string Details => $"{Name} on {Host} site id {SiteId}";
    }
}
