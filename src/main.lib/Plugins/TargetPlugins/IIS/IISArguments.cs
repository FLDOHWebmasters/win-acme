using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISArguments : BaseArguments
    {
        public override string Name => "IIS plugin";
        public override string Group => "Target";
        public override string Condition => "--source iis";

        [CommandLine(Description = "Identifiers of one or more sites to include. This may be a comma-separated list.")]
        public string? SiteId { get; set; }

        [CommandLine(Description = "Host name to filter. This parameter may be used to target specific bindings. This may be a comma-separated list.")]
        public string? Host { get; set; }

        [CommandLine(Name = "host-pattern", Description = "Pattern filter for host names. Can be used to dynamically include bindings based on their match with the pattern." + MainArguments.IISPatternExamples)]
        public string? Pattern { get; set; }

        [CommandLine(Name = "host-regex", Description = "Regex pattern filter for host names. Some people, when confronted with a " +
                "problem, think \"I know, I'll use regular expressions.\" Now they have two problems.")]
        public string? Regex { get; set; }

        [CommandLine(Description = "Specify the common name of the certificate that should be requested " +
            "for the source. By default this will be the first binding that is enumerated.")]
        public string? CommonName { get; set; }

        [CommandLine(Description = "Exclude host names from the certificate. This may be a comma-separated list.")]
        public string? ExcludeBindings { get; set; }
    }
}
