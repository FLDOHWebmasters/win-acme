using System.Net;
using System.Threading.Tasks;
using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
	public class CitrixAdcOptionsFactory : InstallationPluginFactory<CitrixAdc, CitrixAdcOptions>
    {
        public override int Order => 6;
        private readonly CitrixAdcClient _adcClient; // TODO validate host exists
        private readonly ArgumentsInputService _arguments;

        public CitrixAdcOptionsFactory(CitrixAdcClient adcClient, ArgumentsInputService arguments)
        {
            _adcClient = adcClient;
            _arguments = arguments;
        }

        private ArgumentResult<string?> NitroHost => _arguments.GetString<CitrixAdcArguments>(x => x.NitroIpAddress)
            .WithDefault(CitrixAdcClient.DefaultNitroHost)
            .Validate(x => Task.FromResult(IPAddress.TryParse(x!, out var y) && _adcClient.GetSummary(x).Result != null), x => $"invalid host address {x}");

        private ArgumentResult<string?> NitroUser => _arguments.GetString<CitrixAdcArguments>(x => x.NitroUsername)
            .WithDefault(CitrixAdcClient.DefaultNitroUsername)
            .Validate(x => Task.FromResult(true), "invalid username");

        private ArgumentResult<string?> NitroPass => _arguments.GetString<CitrixAdcArguments>(x => x.NitroPassword)
            .WithDefault(CitrixAdcClient.DefaultNitroPasswordProtected)
            .Validate(x => Task.FromResult(true), "invalid password");

        public override async Task<CitrixAdcOptions> Acquire(Target target, IInputService input, RunLevel runLevel)
        {
            return new CitrixAdcOptions
            {
                NitroHost = await NitroHost.Interactive(input).GetValue(),
                NitroUser = await NitroUser.Interactive(input).GetValue(),
                NitroPass = ProtectedString.ClearPrefix + await NitroPass.Interactive(input).GetValue(),
            };
        }

        public override async Task<CitrixAdcOptions> Default(Target target)
        {
            return new CitrixAdcOptions
            {
                NitroHost = await NitroHost.GetValue(),
                NitroUser = await NitroUser.GetValue(),
                NitroPass = await NitroPass.GetValue(),
            };
        }
    }
}
