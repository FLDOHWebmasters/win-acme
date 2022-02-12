using System.IO;
using System.Threading.Tasks;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    public class FileWatcherOptionsFactory : InstallationPluginFactory<FileWatcher, FileWatcherOptions>
    {
        public override int Order => 6;
        private readonly ILogService _log;
        private readonly ArgumentsInputService _arguments;

        public FileWatcherOptionsFactory(ILogService log, ArgumentsInputService arguments)
        {
            _log = log;
            _arguments = arguments;
        }

        private ArgumentResult<string> Path => _arguments.GetString<FileWatcherArguments>(x => x.UncPath)
            .Required().Validate(x => Task.FromResult(x.NotBlank() && new DirectoryInfo(x!).Exists), "invalid UNC path")!;

        public override async Task<FileWatcherOptions> Acquire(Target target, IInputService input, RunLevel runLevel)
            => new FileWatcherOptions { Path = await Path.Interactive(input).GetValue() };

        public override async Task<FileWatcherOptions> Default(Target target) => new FileWatcherOptions { Path = await Path.GetValue() };
    }
}
