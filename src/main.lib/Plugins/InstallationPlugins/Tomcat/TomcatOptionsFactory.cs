using System.Net;
using System.Threading.Tasks;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    public class TomcatOptionsFactory : InstallationPluginFactory<Tomcat, TomcatOptions>
    {
        public override int Order => 6;
        private readonly ArgumentsInputService _arguments;
        private readonly DomainParseService _domainParseService;

        public TomcatOptionsFactory(ArgumentsInputService arguments, DomainParseService domainParseService)
        {
            _arguments = arguments;
            _domainParseService = domainParseService;
        }

        private ArgumentResult<string?> TomcatHost => _arguments.GetString<TomcatArguments>(x => x.TomcatHost)
            .Validate(x => Task.FromResult(IPAddress.TryParse(x!, out var y) || !string.IsNullOrEmpty(_domainParseService.GetRegisterableDomain(x!))), x => $"invalid host address {x}");

        public override async Task<TomcatOptions> Acquire(Target target, IInputService input, RunLevel runLevel)
            => new TomcatOptions { HostName = await TomcatHost.Interactive(input).GetValue() };

        public override async Task<TomcatOptions> Default(Target target)
            => new TomcatOptions { HostName = await TomcatHost.GetValue() };
    }
}
