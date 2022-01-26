using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    public class RemoteIISHelperArguments : BaseArguments
    {
        public const string IISHostParameterName = "iishost";

        public override string Name => "IIS Web Helper App plugin";
        public override string Group => "Installation";
        public override string Condition => "--installation iishelper";

        [CommandLine(Name = IISHostParameterName, Description = "Host name or IP address of server where IIS and Helper App are running.")]
        public string? IISHost { get; set; }

        [CommandLine(Name = IISWebArguments.SiteIdParameterName, Description = "Specify site to install new bindings to. Defaults to the source if that is an IIS site.")]
        public long? InstallationSiteId { get; set; }

        [CommandLine(Name = IISWebArguments.SslPortParameterName, Description = "Port number to use for newly created HTTPS bindings. Defaults to " + IISWebClient.DefaultBindingPortFormat + ".")]
        public int? SSLPort { get; set; }

        [CommandLine(Name = IISWebArguments.SslIpParameterName, Description = "IP address to use for newly created HTTPS bindings. Defaults to " + IISWebClient.DefaultBindingIp + ".")]
        public string? SSLIPAddress { get; set; }
    }
}
