using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    public class FileWatcherArguments : BaseArguments
    {
        public override string Name => "File Watcher plugin";
        public override string Group => "Installation";
        public override string Condition => "--installation watcher";
    }
}
