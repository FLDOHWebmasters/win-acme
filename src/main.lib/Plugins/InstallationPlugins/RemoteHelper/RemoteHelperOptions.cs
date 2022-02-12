using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    [Plugin("64b99181-ffdd-447b-b3bd-07030ecf8944")]
    public class RemoteHelperOptions : InstallationPluginOptions<RemoteHelper>
    {
        public string? HelperHost { get; set; }
        public string? InstallationSite { get; set; }
        public string? NewBindingIp { get; set; }
        public int? NewBindingPort { get; set; }

        public override string Name => "Helper";
        public override string Description => "Update a certificate using a Certificate Manager helper app running on the target server machine";
        public override string Details => $"{Name} on {HelperHost} site {InstallationSite}";
    }
}
