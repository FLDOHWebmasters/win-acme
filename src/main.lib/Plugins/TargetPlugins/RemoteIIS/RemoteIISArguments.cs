using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    public class RemoteIISArguments : BaseArguments
    {
        public override string Name => "IIS Helper App plugin";
        public override string Group => "Target";
        public override string Condition => "--source helper";

        [CommandLine(Description = "Specify the common name of the certificate. If not provided the first host name will be used.")]
        public string? CommonName { get; set; }

        [CommandLine(Description = "A host name to get a certificate for. This may be a comma-separated list.")]
        public string? Host { get; set; }
    }
}
