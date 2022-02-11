using System.Threading.Tasks;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    public class FileWatcherOptionsFactory : InstallationPluginFactory<FileWatcher, FileWatcherOptions>
    {
        public override int Order => 6;
        private readonly ArgumentsInputService _arguments;
        private readonly DomainParseService _domainParseService;

        public FileWatcherOptionsFactory(ArgumentsInputService arguments, DomainParseService domainParseService)
        {
            _arguments = arguments;
            _domainParseService = domainParseService;
        }

        public override async Task<FileWatcherOptions> Acquire(Target target, IInputService input, RunLevel runLevel) => new FileWatcherOptions();
        public override async Task<FileWatcherOptions> Default(Target target) => new FileWatcherOptions();
    }
}
