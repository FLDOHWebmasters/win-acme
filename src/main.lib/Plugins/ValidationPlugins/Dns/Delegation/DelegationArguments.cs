using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class DelegationArguments : BaseArguments
    {
        public override string Name => "Delegation";
        public override string Group => "Validation";
        public override string Condition => "--validation delegation";

        [CommandLine(Description = "DNS zone for dns-01 validation by delegation")]
        public string? DnsZone { get; set; }
    }
}
