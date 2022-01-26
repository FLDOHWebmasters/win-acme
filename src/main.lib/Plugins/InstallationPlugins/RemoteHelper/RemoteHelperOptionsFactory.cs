using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
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

        private ArgumentResult<string> Site => _arguments
            .GetString<RemoteHelperArguments>(x => x.InstallationSite)
            .Validate(x => Task.FromResult(x.NotBlank()), "invalid site")!;

        private ArgumentResult<int?> NewBindingPort => _arguments.
            GetInt<RemoteIISHelperArguments>(x => x.SSLPort).
            WithDefault(IISWebClient.DefaultBindingPort).
            DefaultAsNull().
            Validate(x => Task.FromResult(x > 1), "invalid port").
            Validate(x => Task.FromResult(x < 65536), "invalid port");

        private ArgumentResult<string?> NewBindingIp => _arguments.
            GetString<RemoteIISHelperArguments>(x => x.SSLIPAddress).
            WithDefault(IISWebClient.DefaultBindingIp).
            DefaultAsNull().
            Validate(x => Task.FromResult(x == "*" || IPAddress.Parse(x!) != null), "invalid address");

        public override async Task<RemoteHelperOptions> Acquire(Target target, IInputService input, RunLevel runLevel)
        => new RemoteHelperOptions
        {
            HelperHost = await HelperHost.Interactive(input).GetValue(),
            InstallationSite = await Site.Interactive(input).GetValue(),
            NewBindingPort = await NewBindingPort.Interactive(input).GetValue(),
            NewBindingIp = await NewBindingIp.Interactive(input).GetValue()
        };

        public override async Task<RemoteHelperOptions> Default(Target target) => new RemoteHelperOptions
        {
            HelperHost = await HelperHost.GetValue(),
            InstallationSite = await Site.GetValue(),
            NewBindingPort = await NewBindingPort.GetValue(),
            NewBindingIp = await NewBindingIp.GetValue(),
        };
    }
}