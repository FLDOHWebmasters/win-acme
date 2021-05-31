using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    [Plugin("847d2a6a-be9a-11eb-8529-0242ac130003")]
    internal class CitrixADCOptions : InstallationPluginOptions<CitrixADC>
    {
        public string? NitroHost { get; set; }
        public string? NitroUser { get; set; }
        public string? NitroPass { get; set; }

        public override string Name => "Citrix ADC";
        public override string Description => "Update a certificate via the Nitro v1 API";
    }
}
