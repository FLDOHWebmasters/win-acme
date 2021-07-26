using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    public class TomcatArguments : BaseArguments
    {
        public const string HostParameterName = "tomcathost";

        public override string Name => "Tomcat plugin";
        public override string Group => "Installation";
        public override string Condition => "--installation tomcat";

        [CommandLine(Name = HostParameterName, Description = "Server name or IP address of Tomcat host.")]
        public string? TomcatHost { get; set; }
    }
}
