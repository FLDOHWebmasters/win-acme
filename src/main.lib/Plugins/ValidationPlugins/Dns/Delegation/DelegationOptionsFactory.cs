using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class DelegationOptionsFactory : ValidationPluginOptionsFactory<Delegation, DelegationOptions>
    {
        private readonly ArgumentsInputService _arguments;
        private readonly DomainParseService _domainParseService;

        public DelegationOptionsFactory(
            DomainParseService domainParseService,
            ArgumentsInputService arguments) : base(Constants.Dns01ChallengeType)
        {
            _arguments = arguments;
            _domainParseService = domainParseService;
        }

        private ArgumentResult<string?> DelegationZone => _arguments.
            GetString<DelegationArguments>(x => x.DnsZone).
            Validate(x => Task.FromResult(!string.IsNullOrEmpty(_domainParseService.GetTLD(x!))), "invalid zone domain").
            Required();

        public override async Task<DelegationOptions?> Acquire(Target target, IInputService input, RunLevel runLevel)
            => new DelegationOptions { Zone = await DelegationZone.Interactive(input).GetValue() };

        public override async Task<DelegationOptions?> Default(Target target)
            => new DelegationOptions { Zone = await DelegationZone.GetValue() };

        public override bool CanValidate(Target target)
            => target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }
}
