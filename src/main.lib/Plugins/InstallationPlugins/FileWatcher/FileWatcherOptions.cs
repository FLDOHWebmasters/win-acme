using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using System;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    [Plugin("fd417f8b-a78e-47f5-85ea-b25af0e94a44")]
    public class FileWatcherOptions : InstallationPluginOptions<FileWatcher>
    {
        public string? Path { get; set; }

        public override string Name => "Watcher";
        public override string Description => "Update a certificate via a file watcher on the host machine";
        public override string Details => $"{Name} on {Path}";
        public override string? HostName => Path != null && Path.StartsWith(@"\\")
            ? Path.Substring(2, Math.Max(0, Path.IndexOf(@"\", 2) - 2)).IfBlank(Path[2..])
            : Path;
    }
}
