using System.Threading.Tasks;
using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class CitrixADCOptionsFactory : InstallationPluginFactory<CitrixADC, CitrixADCOptions>
    {
        private static string ClearPrefix => ProtectedString.ClearPrefix;
        private static string EncryptedPrefix => ProtectedString.EncryptedPrefix;

        public override int Order => 6;
        private readonly CitrixAdcClient _adcClient;
        private readonly ArgumentsInputService _arguments;

        public CitrixADCOptionsFactory(CitrixAdcClient adcClient, ArgumentsInputService arguments)
        {
            _adcClient = adcClient;
            _arguments = arguments;
        }

        private ArgumentResult<string?> NitroHost => _arguments.GetString<CitrixADCArguments>(x => x.NitroIpAddress)
            .WithDefault(CitrixAdcClient.DefaultNitroHost).DefaultAsNull();
            //.Validate(x => Task.FromResult(x >= 1), "invalid port");

        private ArgumentResult<string?> NitroUser => _arguments.GetString<CitrixADCArguments>(x => x.NitroUsername)
            .WithDefault(CitrixAdcClient.DefaultNitroUsername).DefaultAsNull();

        private ArgumentResult<string?> NitroPass => _arguments
            .GetString<CitrixADCArguments>(x => (x.NitroPassword != null && x.NitroPassword.StartsWith(EncryptedPrefix) ? "" : ClearPrefix) + x.NitroPassword)
            .WithDefault(CitrixAdcClient.DefaultNitroPasswordProtected).DefaultAsNull();

        public override async Task<CitrixADCOptions> Acquire(Target target, IInputService input, RunLevel runLevel)
        {
            return new CitrixADCOptions
            {
                NitroHost = await NitroHost.Interactive(input).GetValue(),
                NitroUser = await NitroUser.Interactive(input).GetValue(),
                NitroPass = await NitroPass.Interactive(input).GetValue(),
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
