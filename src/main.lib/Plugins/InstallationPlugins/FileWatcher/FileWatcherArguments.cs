using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    public class FileWatcherArguments : BaseArguments
    {
        public const string PathParameterName = "uncpath";

        public override string Name => "File Watcher plugin";
        public override string Group => "Installation";
        public override string Condition => "--installation watcher";

        [CommandLine(Name = PathParameterName, Description = "UNC Path where the certificate file will be watched for updates.")]
        public string? UncPath { get; set; }
    }
}
