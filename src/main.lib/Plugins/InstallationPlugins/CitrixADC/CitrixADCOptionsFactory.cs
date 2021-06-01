using System.Threading.Tasks;
using PKISharp.WACS.Clients.CitrixADC;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class CitrixADCOptionsFactory : InstallationPluginFactory<CitrixADC, CitrixADCOptions>
    {
        public override int Order => 6;
        private readonly CitrixADCClient _adcClient;
        private readonly ArgumentsInputService _arguments;

        public CitrixADCOptionsFactory(CitrixADCClient adcClient, ArgumentsInputService arguments)
        {
            _adcClient = adcClient;
            _arguments = arguments;
        }

        private ArgumentResult<string?> NitroHost => _arguments.GetString<CitrixADCArguments>(x => x.NitroIpAddress)
            .WithDefault(CitrixADCClient.DefaultNitroHost).DefaultAsNull();
            //.Validate(x => Task.FromResult(x >= 1), "invalid port");

        private ArgumentResult<string?> NitroUser => _arguments.GetString<CitrixADCArguments>(x => x.NitroUsername)
            .WithDefault(CitrixADCClient.DefaultNitroUsername).DefaultAsNull();

        private ArgumentResult<string?> NitroPass => _arguments.GetString<CitrixADCArguments>(x => x.NitroPassword)
            .WithDefault(CitrixADCClient.DefaultNitroPasswordProtected).DefaultAsNull();

        public override async Task<CitrixADCOptions> Acquire(Target target, IInputService input, RunLevel runLevel)
        {
            return new CitrixADCOptions
            {
                NitroHost = await NitroHost.Interactive(input).GetValue(),
                NitroUser = await NitroUser.Interactive(input).GetValue(),
                NitroPass = ProtectedString.ClearPrefix + await NitroPass.Interactive(input).GetValue(),
            };
        }

        public override async Task<CitrixADCOptions> Default(Target target)
        {
            return new CitrixADCOptions
            {
                NitroHost = await NitroHost.GetValue(),
                NitroUser = await NitroUser.GetValue(),
                NitroPass = await NitroPass.GetValue(),
            };
        }
    }
}
