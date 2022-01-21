using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    public class HelperAppArguments : BaseArguments
    {
        public const string HostParameterName = "helperhost";

        public override string Name => "Helper App plugin";
        public override string Group => "Installation";
        public override string Condition => "--installation helper";

        [CommandLine(Name = HostParameterName, Description = "Host name or IP address of server where Helper App is running.")]
        public string? Host { get; set; }
    }
}
