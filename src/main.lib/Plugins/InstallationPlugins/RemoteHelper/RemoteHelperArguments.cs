using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    public class RemoteHelperArguments : BaseArguments
    {
        public const string HostParameterName = "helperhost";
        public const string SiteParameterName = "installationsite";

        public override string Name => "Helper App plugin";
        public override string Group => "Installation";
        public override string Condition => "--installation helper";

        [CommandLine(Name = HostParameterName, Description = "Host name or IP address of server where Helper App is running.")]
        public string? Host { get; set; }

        [CommandLine(Name = SiteParameterName, Description = "Specify name/location of site where to install/bind certificate.")]
        public string? InstallationSite { get; set; }

        [CommandLine(Name = IISWebArguments.SslPortParameterName, Description = "Port number to use for newly created HTTPS bindings. Defaults to " + IISWebClient.DefaultBindingPortFormat + ".")]
        public int? SSLPort { get; set; }

        [CommandLine(Name = IISWebArguments.SslIpParameterName, Description = "IP address to use for newly created HTTPS bindings. Defaults to " + IISWebClient.DefaultBindingIp + ".")]
        public string? SSLIPAddress { get; set; }
    }
}
