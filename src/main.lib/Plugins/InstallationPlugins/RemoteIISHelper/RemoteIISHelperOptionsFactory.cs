using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    public class RemoteIISHelperOptionsFactory : InstallationPluginFactory<RemoteIISHelper, RemoteIISHelperOptions>
    {
        public override int Order => 6;
        private readonly IISRemoteHelperClient _helperApp;
        private readonly ArgumentsInputService _arguments;
        private readonly Regex _hostRegex = new(@"^[a-z][a-z0-9\.-]+$");

        public RemoteIISHelperOptionsFactory(IISRemoteHelperClient helperApp, ArgumentsInputService arguments)
        {
            _helperApp = helperApp;
            _arguments = arguments;
        }

        public override bool CanInstall(IEnumerable<Type> storeTypes) =>
            storeTypes.Contains(typeof(CertificateStore));

        private ArgumentResult<string?> Host => _arguments.
            GetString<RemoteIISHelperArguments>(x => x.IISHost).
            DefaultAsNull().
            Validate(x => Task.FromResult(IPAddress.TryParse(x!, out var y) || _hostRegex.IsMatch(x!)), x => $"invalid host address {x}");

        private ArgumentResult<int?> NewBindingPort => _arguments.
            GetInt<RemoteIISHelperArguments>(x => x.SSLPort).
            WithDefault(IISWebClient.DefaultBindingPort).
            DefaultAsNull().
            Validate(x => Task.FromResult(x >= 1), "invalid port").
            Validate(x => Task.FromResult(x <= 65535), "invalid port");

        private ArgumentResult<string?> NewBindingIp => _arguments.
            GetString<RemoteIISHelperArguments>(x => x.SSLIPAddress).
            WithDefault(IISWebClient.DefaultBindingIp).
            DefaultAsNull().
            Validate(x => Task.FromResult(x == "*" || IPAddress.Parse(x!) != null), "invalid address");

        private ArgumentResult<long?> InstallationSite => _arguments.
            GetLong<RemoteIISHelperArguments>(x => x.InstallationSiteId).
            Validate(x => Task.FromResult(_helperApp.GetWebSite(x!.Value) != null), "invalid site");

        public override async Task<RemoteIISHelperOptions> Acquire(Target target, IInputService inputService, RunLevel runLevel)
        {
            var ret = new RemoteIISHelperOptions()
            {
                Host = await Host.GetValue(),
                NewBindingPort = await NewBindingPort.GetValue(),
                NewBindingIp = await NewBindingIp.GetValue()
            };
            var ask = true;
            if (target.IIS)
            {
                ask = runLevel.HasFlag(RunLevel.Advanced) &&
                    await inputService.PromptYesNo("Use different site for installation?", false);
            }
            if (ask)
            {
                var chosen = await inputService.ChooseRequired("Choose site to create new bindings",
                   _helperApp.WebSites,
                   x => Choice.Create(x.Id, x.Name, x.Id.ToString()));
                ret.SiteId = chosen;
            }
            return ret;
        }

        public override async Task<RemoteIISHelperOptions> Default(Target target) => new RemoteIISHelperOptions
        {
            Host = await Host.GetValue(),
            NewBindingPort = await NewBindingPort.GetValue(),
            NewBindingIp = await NewBindingIp.GetValue(),
            SiteId = await InstallationSite.Required(!target.IIS).GetValue()
        };
    }
}
