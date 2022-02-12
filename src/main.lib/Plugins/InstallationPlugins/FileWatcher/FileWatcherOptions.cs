using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    [Plugin("fd417f8b-a78e-47f5-85ea-b25af0e94a44")]
    public class FileWatcherOptions : InstallationPluginOptions<FileWatcher>
    {
        public string? Path { get; set; }

        public override string Name => "Watcher";
        public override string Description => "Update a certificate via a file watcher on the host machine";
    }
}
