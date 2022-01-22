using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class RemoteHelperOptionsFactory : InstallationPluginFactory<RemoteHelper, RemoteHelperOptions>
    {
        private readonly ILogService _log;
        private readonly ArgumentsInputService _arguments;
        private readonly Regex _hostRegex = new(@"^[a-z][a-z0-9\.-]+$");

        public RemoteHelperOptionsFactory(ILogService log, ArgumentsInputService arguments)
        {
            _log = log;
            _arguments = arguments;
        }

        private bool ValidateHost(string? x) => (_hostRegex.IsMatch(x!) || IPAddress.TryParse(x!, out var _)) && RemoteHelperClient.Exists(x!, _log);

        private ArgumentResult<string?> HelperHost => _arguments.GetString<RemoteHelperArguments>(x => x.Host)
            .Required().Validate(x => Task.FromResult(ValidateHost(x)), x => $"invalid host address {x}");

        public override async Task<RemoteHelperOptions> Acquire(Target target, IInputService input, RunLevel runLevel) =>
            new RemoteHelperOptions { HelperHost = await HelperHost.Interactive(input).GetValue() };

        public override async Task<RemoteHelperOptions> Default(Target target) =>
            new RemoteHelperOptions { HelperHost = await HelperHost.GetValue() };
    }
}